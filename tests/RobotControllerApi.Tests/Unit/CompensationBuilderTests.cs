using System.Text.Json.Nodes;
using FluentAssertions;
using RobotControllerApi.BoundedContexts.Shared;

namespace RobotControllerApi.Tests.Unit;

// CompensationBuilder.TransformPayload is the fifth pure function under test.
//
// Chosen because it is genuinely pure - static, no data access, no clock, no I/O, and
// documented in the source as "pure by design" - while carrying real consequence: it
// derives the command that undoes a command. The robot caches that inverse locally and
// replays it after losing connection, with none of the server-side pre-flight checks
// that normally catch a bad step. If the negation is wrong here, a rollback drives the
// robot further in the direction it was meant to reverse out of.
public class CompensationBuilderTests
{
    private static CompensationCommand DriveDistance() =>
        new(Name: "DRIVE_DISTANCE",
            ExecutionKind: "Grid",
            RollbackKind: "Exact",
            InverseCommandName: "DRIVE_DISTANCE",
            RequiresDuration: false);

    [Fact]
    public void TransformPayload_NegatesTheMagnitudeWhenACommandIsItsOwnInverse()
    {
        var command = DriveDistance();

        var inversePayload = CompensationBuilder.TransformPayload(
            original: command,
            inverse: command,
            payloadJson: """{"distanceCm": 40}""",
            durationMs: null,
            rollbackKind: "Exact");

        var node = JsonNode.Parse(inversePayload)!.AsObject();

        node["distanceCm"]!.GetValue<double>().Should().Be(-40);
    }

    [Theory]
    [InlineData("Exact", true)]
    [InlineData("exact", true)]
    [InlineData("BestEffort", false)]
    [InlineData("None", false)]
    public void IsExact_MatchesOnlyTheExactKind_IgnoringCase(string rollbackKind, bool expected)
    {
        CompensationBuilder.IsExact(rollbackKind).Should().Be(expected);
    }

    [Fact]
    public void ResolveRollbackKind_PrefersTheKindCapturedInHistoryOverTheCatalogue()
    {
        // Replaying old work must use the rules that applied when it ran, not whatever
        // the catalogue says today.
        var current = DriveDistance() with { RollbackKind = "None" };

        CompensationBuilder.ResolveRollbackKind("BestEffort", current).Should().Be("BestEffort");
        CompensationBuilder.ResolveRollbackKind(null, current).Should().Be("None");
        CompensationBuilder.ResolveRollbackKind("   ", current).Should().Be("None");
    }
}
