namespace RobotControllerApi.BoundedContexts.JobHistories.Dtos;

public class JobHistoryResponse
{
    public int Id { get; set; }
    public int? JobId { get; set; }
    public int? WorkflowId { get; set; }
    public int? StepNumber { get; set; }
    public int DeviceId { get; set; }
    public int? CommandCatalogueId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string ProviderType { get; set; } = "Api";
    public string ExecutionKind { get; set; } = "Mode";
    public string RollbackKind { get; set; } = "None";
    public bool Executed { get; set; }
    public bool Success { get; set; }
    public string? ResultJson { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationMs { get; set; }
    public int? RollbackOfJobHistoryId { get; set; }
    public DateTime CreatedDate { get; set; }
}
