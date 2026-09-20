using System.Text;
using System.Text.Json;

using Spectre.Console;

namespace RobotControllerApi.Infrastructure.Logging;

/// <summary>
/// Turns a raw request/response payload into Spectre markup: pretty-printed and colour-coded
/// when the body is JSON, summarised when it is too big to read, escaped plain text otherwise.
///
/// Two rules keep this safe to point at production traffic:
///  - every literal that reaches the console goes through <see cref="Markup.Escape"/>, because
///    JSON arrays are full of '[' and Spectre reads that as the start of a style tag;
///  - a property whose name looks like a credential is never printed, whatever its value is.
/// </summary>
internal static class JsonMarkupFormatter
{
    private const string KeyColour = "aqua";
    private const string StringColour = "green";
    private const string NumberColour = "fuchsia";
    private const string BooleanColour = "yellow";
    private const string NullColour = "grey";
    private const string PunctuationColour = "grey";
    private const string RedactedColour = "red";

    private const int MaxDepth = 12;

    // Above this, do not even try to parse for a summary - just cut the text. Guards against
    // spending real CPU on a pathological payload.
    private const int MaxParseableChars = 1_000_000;

    private static readonly string OpenBracket = Markup.Escape("[");
    private static readonly string CloseBracket = Markup.Escape("]");

    // Matched as substrings against the property name, case-insensitively.
    //
    // "hash" is deliberately NOT here: it matched hashAlgorithm and hid a plain algorithm name
    // like HMACSHA256, which is not a secret and is worth seeing. Names that genuinely carry a
    // secret already match via "password" (passwordHash) or "secret" (secretHash).
    private static readonly string[] SensitiveNameFragments =
    {
        "password", "secret", "token", "salt", "apikey", "api_key", "authorization", "hashkey"
    };

    // Matched against the whole property name. A field called exactly "hash" is a digest.
    private static readonly string[] SensitiveExactNames =
    {
        "hash"
    };

    public static string Format(string body, string? contentType, int maxBodyChars)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"[{NullColour}](empty)[/]";
        }

        // Summarise before formatting: a finished markup string cannot be cut in half without
        // risking slicing a style tag, and a 200 KB wall of raw text helps nobody.
        if (body.Length > maxBodyChars)
        {
            return SummariseLargeBody(body, contentType, maxBodyChars);
        }

        if (!LooksLikeJson(contentType, body))
        {
            return $"[silver]{Markup.Escape(body)}[/]";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var builder = new StringBuilder();
            WriteElement(document.RootElement, builder, 0);
            return builder.ToString();
        }
        catch (JsonException)
        {
            // Content-Type claimed JSON but the payload is not. Show it verbatim.
            return $"[silver]{Markup.Escape(body)}[/]";
        }
    }

    /// <summary>
    /// Describes an oversized payload instead of dumping it: shape, item count and size, plus
    /// the first element when it is small enough to be useful. The unpaginated dashboard reads
    /// run to a couple of hundred KB, and a truncated slab of that was worse than nothing.
    /// </summary>
    private static string SummariseLargeBody(string body, string? contentType, int maxBodyChars)
    {
        if (LooksLikeJson(contentType, body) && body.Length <= MaxParseableChars)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var size = DescribeSize(body.Length);

                if (root.ValueKind == JsonValueKind.Array)
                {
                    var count = root.GetArrayLength();
                    var header = $"[{NullColour}]array, {count} items, {size} - too large to print[/]";

                    if (count == 0)
                    {
                        return header;
                    }

                    var preview = PreviewElement(root[0], maxBodyChars);
                    return preview is null
                        ? header
                        : header + $"\n[{NullColour}]first item:[/]\n" + preview;
                }

                if (root.ValueKind == JsonValueKind.Object)
                {
                    var count = 0;
                    foreach (var _ in root.EnumerateObject())
                    {
                        count++;
                    }

                    return $"[{NullColour}]object, {count} properties, {size} - too large to print[/]";
                }
            }
            catch (JsonException)
            {
                // Falls through to the plain-text cut below.
            }
        }

        var shown = Markup.Escape(body[..maxBodyChars]);
        var omitted = body.Length - maxBodyChars;
        return $"[silver]{shown}[/]\n[{NullColour}]... truncated, {omitted} more characters[/]";
    }

    private static string? PreviewElement(JsonElement element, int maxBodyChars)
    {
        var budget = Math.Max(200, maxBodyChars / 2);

        if (element.GetRawText().Length > budget)
        {
            return null;
        }

        var builder = new StringBuilder();
        WriteElement(element, builder, 0);
        return builder.ToString();
    }

    private static string DescribeSize(int characters)
    {
        if (characters >= 1024 * 1024)
        {
            return (characters / 1024d / 1024d).ToString("0.#") + " MB";
        }

        return characters >= 1024
            ? (characters / 1024d).ToString("0.#") + " KB"
            : characters + " chars";
    }

    private static bool LooksLikeJson(string? contentType, string body)
    {
        if (contentType is not null && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var trimmed = body.AsSpan().TrimStart();
        return trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '[');
    }

    private static bool IsSensitive(string propertyName)
    {
        foreach (var exact in SensitiveExactNames)
        {
            if (string.Equals(propertyName, exact, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var fragment in SensitiveNameFragments)
        {
            if (propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteElement(JsonElement element, StringBuilder builder, int depth)
    {
        if (depth > MaxDepth)
        {
            builder.Append($"[{NullColour}]...[/]");
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(element, builder, depth);
                return;

            case JsonValueKind.Array:
                WriteArray(element, builder, depth);
                return;

            case JsonValueKind.String:
                builder.Append($"[{StringColour}]\"{Markup.Escape(element.GetString() ?? string.Empty)}\"[/]");
                return;

            case JsonValueKind.Number:
                builder.Append($"[{NumberColour}]{Markup.Escape(element.GetRawText())}[/]");
                return;

            case JsonValueKind.True:
            case JsonValueKind.False:
                builder.Append($"[{BooleanColour}]{Markup.Escape(element.GetRawText())}[/]");
                return;

            default:
                builder.Append($"[{NullColour}]null[/]");
                return;
        }
    }

    private static void WriteObject(JsonElement element, StringBuilder builder, int depth)
    {
        var padding = new string(' ', depth * 2);
        var first = true;

        foreach (var property in element.EnumerateObject())
        {
            if (first)
            {
                builder.Append($"[{PunctuationColour}]{{[/]");
                first = false;
            }
            else
            {
                builder.Append($"[{PunctuationColour}],[/]");
            }

            builder.Append('\n').Append(padding).Append("  ");
            builder.Append($"[{KeyColour}]\"{Markup.Escape(property.Name)}\"[/][{PunctuationColour}]: [/]");

            if (IsSensitive(property.Name))
            {
                // Redact the whole subtree, not just scalars: a "credential" object would
                // otherwise leak its children.
                builder.Append($"[{RedactedColour}]\"<redacted>\"[/]");
            }
            else
            {
                WriteElement(property.Value, builder, depth + 1);
            }
        }

        if (first)
        {
            builder.Append($"[{PunctuationColour}]{{}}[/]");
            return;
        }

        builder.Append('\n').Append(padding).Append($"[{PunctuationColour}]}}[/]");
    }

    private static void WriteArray(JsonElement element, StringBuilder builder, int depth)
    {
        var padding = new string(' ', depth * 2);
        var first = true;

        foreach (var item in element.EnumerateArray())
        {
            if (first)
            {
                builder.Append($"[{PunctuationColour}]{OpenBracket}[/]");
                first = false;
            }
            else
            {
                builder.Append($"[{PunctuationColour}],[/]");
            }

            builder.Append('\n').Append(padding).Append("  ");
            WriteElement(item, builder, depth + 1);
        }

        if (first)
        {
            builder.Append($"[{PunctuationColour}]{OpenBracket}{CloseBracket}[/]");
            return;
        }

        builder.Append('\n').Append(padding).Append($"[{PunctuationColour}]{CloseBracket}[/]");
    }
}
