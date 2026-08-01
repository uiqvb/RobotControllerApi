namespace RobotControllerApi.BoundedContexts.Jobs.Dtos;

// Sent by a robot when it reconnects after rolling itself back while the backend was
// unreachable. While offline the robot moved and nothing reported it, so the backend's
// idea of where the robot is has gone stale. This is the robot telling it what happened.
public class ReportOfflineRollbackRequest
{
    // In the order the robot executed them, oldest first.
    public List<OfflineRollbackStepReport> Steps { get; set; } = new();

    // Free text for the audit trail, e.g. "backend unreachable for 30s".
    public string? Reason { get; set; }
}

public class OfflineRollbackStepReport
{
    // The inverse the robot executed, e.g. STEP_BACK or LEFT. This is the command name the
    // robot was handed in InverseCommandName when it claimed the original work.
    public string CommandName { get; set; } = string.Empty;

    public string? PayloadJson { get; set; }

    // The job this step undid, when the robot still knows it. Lets the backend mark the
    // original work RolledBack instead of leaving it stuck.
    public int? RollbackOfJobId { get; set; }

    public DateTime? ExecutedAtUtc { get; set; }
}
