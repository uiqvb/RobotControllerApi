namespace RobotControllerApi.Infrastructure.Logging;

/// <summary>
/// Bound from the "RequestLogging" configuration section. Every knob has a working
/// default, so an appsettings.json without the section still behaves sensibly.
/// </summary>
public class RequestLoggingOptions
{
    /// <summary>Master switch. Turn this off before running throughput benchmarks.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Bodies longer than this are summarised instead of printed.</summary>
    public int MaxBodyChars { get; set; } = 4000;

    /// <summary>Print the full request header list. Noisy; the actor line usually tells you enough.</summary>
    public bool ShowHeaders { get; set; } = false;

    /// <summary>Print the request body panel.</summary>
    public bool ShowRequestBody { get; set; } = true;

    /// <summary>Print the response body panel.</summary>
    public bool ShowResponseBody { get; set; } = true;

    /// <summary>Requests whose path starts with any of these (case-insensitive) are not logged.</summary>
    public string[] IgnorePathPrefixes { get; set; } =
    {
        "/swagger",
        "/favicon.ico",
        "/_framework"
    };

    /// <summary>
    /// Rules that hide the panel for a request that turned out to be uneventful, while still
    /// showing the one that actually did something. See <see cref="QuietPollingRule"/>.
    /// </summary>
    public QuietPollingRule[] QuietPolling { get; set; } =
    {
        // The Nano polls this every 100 ms (LIVE_CONTROL_POLL_INTERVAL_MS in the sketch) and
        // "nothing to do" comes back as 200 + CommandName=STOP + IsExpired=true, not as 204,
        // so the status code alone cannot tell the two apart.
        //
        // The path filter is anchored at both ends because the dashboard's WASD hold PUTs to
        // /api/devices/{id}/live-control-command, which shares the same tail.
        new()
        {
            PathStartsWith = "/api/adapter/devices/",
            PathEndsWith = "/live-control-command",
            Method = "GET",
            WhenResponseJsonProperty = "isExpired",
            EqualsValue = "true",
            Because = "no live command"
        },

        // Polled every 1200 ms (COMMAND_POLL_INTERVAL_MS). This one answers honestly with 204.
        new()
        {
            PathEndsWith = "/work-items/claim-next",
            Method = "POST",
            WhenStatusIn = new[] { 204 },
            Because = "no queued work"
        },

        // The dashboard's refresh() fires these six in parallel every second (POLL_MS in
        // wwwroot/index.html). They always return data, so there is no "empty" answer to key
        // on and the path itself has to be muted. Restricted to GET + 200 so the POSTs that
        // create jobs and workflows, and any failure, still print.
        //
        // Anchored to /api/devices/{id}/... on purpose: a bare "/jobs" would also swallow
        // GET /api/jobs and GET /api/jobs/{id}.
        new() { PathStartsWith = "/api/devices/", PathEndsWith = "/capabilities", Method = "GET", WhenStatusIn = new[] { 200 }, Because = "dashboard refresh" },
        new() { PathStartsWith = "/api/devices/", PathEndsWith = "/jobs", Method = "GET", WhenStatusIn = new[] { 200 }, Because = "dashboard refresh" },
        new() { PathStartsWith = "/api/devices/", PathEndsWith = "/workflows", Method = "GET", WhenStatusIn = new[] { 200 }, Because = "dashboard refresh" },
        new() { PathStartsWith = "/api/devices/", PathEndsWith = "/job-history", Method = "GET", WhenStatusIn = new[] { 200 }, Because = "dashboard refresh" },
        new() { PathEndsWith = "/api/device-status", Method = "GET", WhenStatusIn = new[] { 200 }, Because = "dashboard refresh" },
        new() { PathEndsWith = "/api/device-credentials", Method = "GET", WhenStatusIn = new[] { 200 }, Because = "dashboard refresh" }
    };

    /// <summary>Print an occasional one-line tally of what was hidden, so a silent console still shows traffic is flowing.</summary>
    public bool ShowSuppressedSummary { get; set; } = true;

    /// <summary>Shortest gap between those tally lines.</summary>
    public int SuppressedSummarySeconds { get; set; } = 10;
}

/// <summary>
/// "For this request, stay quiet unless something actually happened."
///
/// A rule matches when every condition it fills in holds, so anything unexpected - a wrong
/// status, an unparseable payload, a different verb - falls through and gets printed as usual.
/// At least one path condition is required; a rule that targets no path never matches.
/// </summary>
public class QuietPollingRule
{
    /// <summary>Case-insensitive substring of the request path.</summary>
    public string PathContains { get; set; } = string.Empty;

    /// <summary>Case-insensitive prefix of the request path. Use with PathEndsWith to pin a route with an id in the middle.</summary>
    public string? PathStartsWith { get; set; }

    /// <summary>Case-insensitive suffix of the request path.</summary>
    public string? PathEndsWith { get; set; }

    /// <summary>HTTP verb this rule applies to. Omit to match any verb.</summary>
    public string? Method { get; set; }

    /// <summary>Top-level property of the JSON response to test, camelCase as serialised. Optional.</summary>
    public string? WhenResponseJsonProperty { get; set; }

    /// <summary>Value that property must equal for the request to count as uneventful. Optional.</summary>
    public string? EqualsValue { get; set; }

    /// <summary>Status codes that count as uneventful, for endpoints that answer "nothing" with 204/404. Optional.</summary>
    public int[]? WhenStatusIn { get; set; }

    /// <summary>Short phrase shown in the tally line, e.g. "no live command".</summary>
    public string Because { get; set; } = "nothing to report";

    /// <summary>True when this rule needs the response body read in order to decide.</summary>
    public bool NeedsResponseBody => !string.IsNullOrWhiteSpace(WhenResponseJsonProperty);

    public bool MatchesRequest(string method, string path)
    {
        var targetsAPath = false;

        if (!string.IsNullOrWhiteSpace(PathContains))
        {
            targetsAPath = true;
            if (!path.Contains(PathContains, StringComparison.OrdinalIgnoreCase)) return false;
        }

        if (!string.IsNullOrWhiteSpace(PathStartsWith))
        {
            targetsAPath = true;
            if (!path.StartsWith(PathStartsWith, StringComparison.OrdinalIgnoreCase)) return false;
        }

        if (!string.IsNullOrWhiteSpace(PathEndsWith))
        {
            targetsAPath = true;
            if (!path.EndsWith(PathEndsWith, StringComparison.OrdinalIgnoreCase)) return false;
        }

        // A rule with no path target would silence the entire API. Refuse rather than obey.
        if (!targetsAPath) return false;

        return string.IsNullOrWhiteSpace(Method)
            || string.Equals(method, Method, StringComparison.OrdinalIgnoreCase);
    }
}
