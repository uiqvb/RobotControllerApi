using System.Text.Json;
using System.Text.Json.Nodes;

namespace RobotControllerApi.BoundedContexts.Shared;

// The catalogue facts the compensation logic needs, and nothing else.
// Both the rollback path (CommandCatalogue) and the dispatch path
// (CommandCatalogueSnapshot) project onto this, so neither owns the logic.
public sealed record CompensationCommand(
    string Name,
    string ExecutionKind,
    string RollbackKind,
    string? InverseCommandName,
    bool RequiresDuration);

// Builds the command that undoes a command.
//
// Two callers, two moments:
//   - rollback generation, after a command has executed and been historised;
//   - work dispatch, before the robot moves, so the claim response can carry the
//     inverse for the robot to cache locally and replay if it loses connection.
//
// Pure by design: no data access, so it can be exercised without a database.
public static class CompensationBuilder
{
    // History rows may carry a rollback kind captured at execution time; it wins over
    // the catalogue's current value so replaying old work uses the rules that applied then.
    public static string ResolveRollbackKind(string? historyRollbackKind, CompensationCommand original)
    {
        return string.IsNullOrWhiteSpace(historyRollbackKind)
            ? original.RollbackKind.Trim()
            : historyRollbackKind.Trim();
    }

    public static bool IsExact(string rollbackKind) => rollbackKind.Equals("Exact", StringComparison.OrdinalIgnoreCase);

    public static bool IsNone(string rollbackKind) => rollbackKind.Equals("None", StringComparison.OrdinalIgnoreCase);

    public static bool IsReversible(string rollbackKind) => IsExact(rollbackKind) || rollbackKind.Equals("BestEffort", StringComparison.OrdinalIgnoreCase);

    // Derives the inverse payload from the original payload.
    // Throws when the inverse needs a duration the original never recorded — the caller
    // decides whether that is fatal (rollback generation) or simply means "no cached
    // inverse for this command" (dispatch).
    public static string TransformPayload(
        CompensationCommand original,
        CompensationCommand inverse,
        string? payloadJson,
        int? durationMs,
        string rollbackKind)
    {
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson) as JsonObject ?? new JsonObject();

        var originalName = original.Name.Trim().ToUpperInvariant();
        var inverseName = inverse.Name.Trim().ToUpperInvariant();

        // A command that is its own inverse reverses by negating its magnitude.
        if (originalName == inverseName)
        {
            if (originalName == "DRIVE_DISTANCE") NegateNumber(node, "distance", "distanceCm", "centimeters", "value");
            if (originalName == "ROTATE_DEGREES") NegateNumber(node, "degrees", "angleDegrees", "value");
        }

        var resolvedDuration = durationMs ?? TryGetInt(node, "durationMs", "duration", "milliseconds", "ms");
        var shouldPreserveDuration =
            inverse.RequiresDuration ||
            original.ExecutionKind.Equals("Continuous", StringComparison.OrdinalIgnoreCase) ||
            inverse.ExecutionKind.Equals("Continuous", StringComparison.OrdinalIgnoreCase) ||
            rollbackKind.Equals("BestEffort", StringComparison.OrdinalIgnoreCase);

        if (shouldPreserveDuration)
        {
            if (!resolvedDuration.HasValue || resolvedDuration.Value <= 0)
            {
                throw new InvalidOperationException($"Rollback command {inverse.Name} requires a valid durationMs value.");
            }

            node["durationMs"] = resolvedDuration.Value;
        }

        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static int? TryGetInt(JsonObject node, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!node.TryGetPropertyValue(name, out var value) || value == null) continue;

            try
            {
                return value.GetValue<int>();
            }
            catch (InvalidOperationException)
            {
                try
                {
                    return Convert.ToInt32(value.GetValue<double>());
                }
                catch
                {
                    // Ignore and continue to the next possible property name.
                }
            }
            catch (FormatException)
            {
                continue;
            }
        }

        return null;
    }

    private static void NegateNumber(JsonObject node, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!node.TryGetPropertyValue(name, out var value) || value == null) continue;
            try
            {
                var number = value.GetValue<double>();
                node[name] = -number;
                return;
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            catch (FormatException)
            {
                continue;
            }
        }
    }
}
