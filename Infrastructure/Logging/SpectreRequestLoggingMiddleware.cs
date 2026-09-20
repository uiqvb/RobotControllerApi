using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

using Spectre.Console;

namespace RobotControllerApi.Infrastructure.Logging;

/// <summary>
/// Prints one Spectre panel per API request: which controller action ran, who called it,
/// the pretty-printed request and response bodies, the status code and how long it took.
///
/// This sits in the pipeline rather than inside the controllers on purpose. There are 18
/// controllers and they would all need the same block of code, which is 18 chances to break
/// something and 18 places to keep in sync. One middleware covers every action that exists
/// today and every action added later, and no controller file changes at all.
/// </summary>
public class SpectreRequestLoggingMiddleware
{
    // Two requests rendering at once would interleave their panel rows into nonsense.
    private static readonly object ConsoleLock = new();

    // Tally of panels held back by a quiet-polling rule, drained into one line periodically.
    private static readonly ConcurrentDictionary<string, int> SuppressedCounts = new();
    private static DateTime _lastSuppressedSummary = DateTime.MinValue;

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<RequestLoggingOptions> _options;

    public SpectreRequestLoggingMiddleware(RequestDelegate next, IOptionsMonitor<RequestLoggingOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var options = _options.CurrentValue;

        if (!options.Enabled || IsIgnored(context.Request.Path, options))
        {
            await _next(context);
            return;
        }

        var quietRules = MatchingQuietRules(context.Request.Method, context.Request.Path.Value ?? string.Empty, options);

        // A rule that inspects the payload still needs the payload, even when response bodies
        // are switched off for display.
        var mustReadResponseBody = options.ShowResponseBody || quietRules.Any(rule => rule.NeedsResponseBody);

        var requestBody = options.ShowRequestBody
            ? await ReadRequestBodyAsync(context.Request, options.MaxBodyChars)
            : string.Empty;

        var originalResponseBody = context.Response.Body;
        using var capturedResponse = new MemoryStream();
        context.Response.Body = capturedResponse;

        var stopwatch = Stopwatch.StartNew();
        Exception? failure = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Record it, then let it travel on untouched. This middleware observes, it never
            // swallows. The finally block still restores the real response stream.
            failure = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var responseBody = string.Empty;

            try
            {
                capturedResponse.Position = 0;

                if (mustReadResponseBody)
                {
                    using var reader = new StreamReader(capturedResponse, Encoding.UTF8, leaveOpen: true);
                    responseBody = await reader.ReadToEndAsync();
                }

                capturedResponse.Position = 0;
                await capturedResponse.CopyToAsync(originalResponseBody);
            }
            finally
            {
                // Must happen even if copying back failed, or the middleware above this one is
                // left holding a disposed MemoryStream.
                context.Response.Body = originalResponseBody;
            }

            try
            {
                // A request that threw is never uneventful, whatever the quiet rules say.
                if (failure is null
                    && quietRules.Length > 0
                    && ShouldStayQuiet(quietRules, context.Response.StatusCode, responseBody, out var reason))
                {
                    NoteSuppressed(context, reason, options);
                }
                else
                {
                    Render(context, requestBody, responseBody, stopwatch.Elapsed, failure, options);
                }
            }
            catch
            {
                // Diagnostics must never be the reason a request fails.
            }
        }
    }

    private static bool IsIgnored(PathString path, RequestLoggingOptions options)
    {
        if (!path.HasValue)
        {
            return false;
        }

        foreach (var prefix in options.IgnorePathPrefixes)
        {
            if (path.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static QuietPollingRule[] MatchingQuietRules(string method, string path, RequestLoggingOptions options)
    {
        if (options.QuietPolling is null || options.QuietPolling.Length == 0 || path.Length == 0)
        {
            return Array.Empty<QuietPollingRule>();
        }

        return options.QuietPolling.Where(rule => rule.MatchesRequest(method, path)).ToArray();
    }

    /// <summary>
    /// True when this response is the "nothing happened" answer the rule was written for.
    /// A rule only matches if every condition it specifies holds, so an unexpected status or
    /// an unexpected payload falls through and gets its panel printed as usual.
    /// </summary>
    private static bool ShouldStayQuiet(
        QuietPollingRule[] rules,
        int status,
        string responseBody,
        out string reason)
    {
        foreach (var rule in rules)
        {
            if (rule.WhenStatusIn is { Length: > 0 } allowed && !allowed.Contains(status))
            {
                continue;
            }

            if (rule.NeedsResponseBody
                && !JsonPropertyEquals(responseBody, rule.WhenResponseJsonProperty!, rule.EqualsValue))
            {
                continue;
            }

            reason = rule.Because;
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool JsonPropertyEquals(string body, string propertyName, string? expected)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var actual = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.GetRawText();

                return expected is null || string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch (JsonException)
        {
            // An error string rather than the expected payload. Unparseable is not uneventful.
            return false;
        }
    }

    /// <summary>
    /// Counts a hidden request and, at most once per configured interval, prints a single dim
    /// line saying what has been held back. Without this a quiet console is ambiguous: you
    /// cannot tell a robot that is polling happily from one that has stopped polling at all.
    /// </summary>
    private static void NoteSuppressed(HttpContext context, string reason, RequestLoggingOptions options)
    {
        if (!options.ShowSuppressedSummary)
        {
            return;
        }

        var key = context.Request.Method + " " + context.Request.Path + "  (" + reason + ")";
        SuppressedCounts.AddOrUpdate(key, 1, (_, count) => count + 1);

        var interval = TimeSpan.FromSeconds(Math.Max(1, options.SuppressedSummarySeconds));
        var now = DateTime.UtcNow;

        lock (ConsoleLock)
        {
            if (now - _lastSuppressedSummary < interval)
            {
                return;
            }

            _lastSuppressedSummary = now;

            foreach (var entry in SuppressedCounts.ToArray())
            {
                if (!SuppressedCounts.TryRemove(entry.Key, out var count) || count == 0)
                {
                    continue;
                }

                AnsiConsole.MarkupLine(
                    "[grey]  quiet  " + Markup.Escape(entry.Key)
                    + " x" + count
                    + "  " + DateTime.Now.ToString("HH:mm:ss") + "[/]");
            }
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, int maxBodyChars)
    {
        if (request.ContentLength is null or 0)
        {
            return string.Empty;
        }

        // Without this the body is a forward-only stream, and reading it here would leave
        // model binding with nothing to bind.
        request.EnableBuffering();

        var buffer = new char[Math.Max(1, maxBodyChars) + 1];
        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);

        request.Body.Position = 0;

        return new string(buffer, 0, read);
    }

    private static void Render(
        HttpContext context,
        string requestBody,
        string responseBody,
        TimeSpan elapsed,
        Exception? failure,
        RequestLoggingOptions options)
    {
        var request = context.Request;
        var response = context.Response;
        var status = response.StatusCode;

        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var actionName = action is null
            ? "(no controller matched)"
            : action.ControllerName + "Controller." + action.ActionName;

        var body = new StringBuilder();

        body.Append("[grey]when   [/] " + Markup.Escape(DateTime.Now.ToString("HH:mm:ss.fff")) + "   ");
        body.Append("[grey]took[/] " + FormatDuration(elapsed) + "   ");
        body.AppendLine("[grey]trace[/] [silver]" + Markup.Escape(context.TraceIdentifier) + "[/]");

        body.AppendLine("[grey]action [/] [white]" + Markup.Escape(actionName) + "[/]");
        body.AppendLine("[grey]caller [/] " + Markup.Escape(DescribeCaller(context)));

        if (request.QueryString.HasValue && request.QueryString.Value.Length > 1)
        {
            body.AppendLine("[grey]query  [/] [silver]" + Markup.Escape(request.QueryString.Value) + "[/]");
        }

        if (options.ShowHeaders)
        {
            body.AppendLine();
            body.AppendLine("[grey]headers[/]");

            foreach (var header in request.Headers.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var value = IsSensitiveHeader(header.Key) ? "<redacted>" : header.Value.ToString();
                body.AppendLine("  [aqua]" + Markup.Escape(header.Key) + "[/][grey]:[/] [silver]" + Markup.Escape(value) + "[/]");
            }
        }

        if (options.ShowRequestBody && requestBody.Length > 0)
        {
            body.AppendLine();
            body.AppendLine("[grey]--- request ---[/]");
            body.AppendLine(JsonMarkupFormatter.Format(requestBody, request.ContentType, options.MaxBodyChars));
        }

        if (options.ShowResponseBody)
        {
            body.AppendLine();
            body.AppendLine("[grey]--- response ---[/]");
            body.AppendLine(JsonMarkupFormatter.Format(responseBody, response.ContentType, options.MaxBodyChars));
        }

        if (failure is not null)
        {
            body.AppendLine();
            body.AppendLine("[red]unhandled " + Markup.Escape(failure.GetType().Name) + ":[/] [silver]" + Markup.Escape(failure.Message) + "[/]");
        }

        var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
        var panelHeader = " " + request.Method + " " + request.Path + query + "  ->  " + status + " ";

        var panel = new Panel(new Markup(body.ToString().TrimEnd()))
            .Header("[bold]" + Markup.Escape(panelHeader) + "[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(AccentFor(status, failure));

        lock (ConsoleLock)
        {
            AnsiConsole.Write(panel);
        }
    }

    private static string DescribeCaller(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var identity = context.User?.Identity;

        if (identity is null || !identity.IsAuthenticated)
        {
            return "anonymous @ " + ip;
        }

        var scheme = identity.AuthenticationType ?? "?";
        var name = string.IsNullOrWhiteSpace(identity.Name) ? "(unnamed)" : identity.Name;

        return name + " via " + scheme + " @ " + ip;
    }

    private static bool IsSensitiveHeader(string name) =>
        name.Contains("authorization", StringComparison.OrdinalIgnoreCase)
        || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || name.Contains("cookie", StringComparison.OrdinalIgnoreCase);

    private static string FormatDuration(TimeSpan elapsed)
    {
        var milliseconds = elapsed.TotalMilliseconds;

        var colour = milliseconds switch
        {
            < 50 => "green",
            < 250 => "yellow",
            _ => "red"
        };

        return "[" + colour + "]" + milliseconds.ToString("0.##") + " ms[/]";
    }

    private static Color AccentFor(int status, Exception? failure)
    {
        if (failure is not null)
        {
            return Color.Red;
        }

        return status switch
        {
            >= 500 => Color.Red,
            >= 400 => Color.Yellow,
            >= 300 => Color.Aqua,
            >= 200 => Color.Green,
            _ => Color.Grey
        };
    }
}
