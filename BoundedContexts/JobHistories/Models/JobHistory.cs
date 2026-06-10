namespace RobotControllerApi.BoundedContexts.JobHistories.Models;

public class JobHistory
{
    public int Id { get; set; } = 0;
    public int? JobId { get; set; }
    public int? WorkflowId { get; set; }
    public int? StepNumber { get; set; }
    public int DeviceId { get; set; }
    public int? CommandCatalogueId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string ProviderType { get; set; } = "Api";

    // Grid, Continuous, Mode, Query.
    public string ExecutionKind { get; set; } = "Mode";

    // Exact, BestEffort, None.
    public string RollbackKind { get; set; } = "None";

    public bool Executed { get; set; }
    public bool Success { get; set; }
    public string? ResultJson { get; set; } = null;
    public string? FailureCode { get; set; } = null;
    public string? FailureMessage { get; set; } = null;

    // Used by timed continuous jobs and workflow steps.
    // Example: MOVE_FORWARD with DurationMs = 30000 can later generate
    // MOVE_BACKWARD with DurationMs = 30000 as best-effort rollback.
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationMs { get; set; }

    public int? RollbackOfJobHistoryId { get; set; }
    public DateTime CreatedDate { get; set; }
}
