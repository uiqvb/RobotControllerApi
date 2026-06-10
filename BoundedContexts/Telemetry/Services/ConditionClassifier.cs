using System.Text.Json;

namespace RobotControllerApi.BoundedContexts.Telemetry.Services;

public class ConditionClassifier : IConditionClassifier
{
    public string ClassifyStatusMessage(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        var mode = GetString(root, "mode");
        var rainDetected = GetBool(root, "rainDetected");
        var luxLeft = GetDouble(root, "luxLeft");
        var luxRight = GetDouble(root, "luxRight");

        if (rainDetected == true)
        {
            return "Rain detected.";
        }

        if (string.Equals(mode, "FOLLOWING_LINE", StringComparison.OrdinalIgnoreCase))
        {
            return "Robot moving to shade.";
        }

        if (string.Equals(mode, "STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            return "Robot stopped.";
        }

        var averageLux = AverageNullable(luxLeft, luxRight);

        if (averageLux == null)
        {
            return "Telemetry received.";
        }

        if (averageLux >= 600)
        {
            return "Good drying conditions.";
        }

        if (averageLux >= 200)
        {
            return "Moderate drying conditions.";
        }

        return "Poor drying conditions.";
    }

    public string ExtractOperationalState(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        var mode = GetString(doc.RootElement, "mode");

        return MapTelemetryModeToOperationalState(mode);
    }

    private static string MapTelemetryModeToOperationalState(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return "Unknown";
        }

        return mode.Trim().ToUpperInvariant() switch
        {
            "IDLE" => "Idle",
            "PAUSED" => "Paused",
            "STOPPED" => "Stopped",
            "FAULTED" => "Faulted",
            "ERROR" => "Faulted",
            "OFFLINE" => "Unknown",

            // Robot-specific runtime modes stay in TelemetryReading.PayloadJson.
            // DeviceStatus.OperationalState remains robot-neutral so it satisfies
            // ck_devicestatus_operationalstate.
            "SUN_TRACKING" => "Executing",
            "FOLLOWING_LINE" => "Executing",
            "RETURNING_TO_SAFE_ZONE" => "Executing",
            "AUTO" => "Executing",
            "FAST" => "Executing",
            "SLOW" => "Executing",
            "MOVING" => "Executing",
            "EXECUTING" => "Executing",

            _ => "Executing"
        };
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool? GetBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) &&
               (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            ? value.GetBoolean()
            : null;
    }

    private static double? GetDouble(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var result)
            ? result
            : null;
    }

    private static double? AverageNullable(double? first, double? second)
    {
        if (first == null && second == null) return null;
        if (first == null) return second;
        if (second == null) return first;
        return (first.Value + second.Value) / 2.0;
    }
}
