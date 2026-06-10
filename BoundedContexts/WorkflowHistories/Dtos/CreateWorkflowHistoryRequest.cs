namespace RobotControllerApi.BoundedContexts.WorkflowHistories.Dtos;

public class CreateWorkflowHistoryRequest
{
    public int? WorkflowId { get; set; }
    public int DeviceId { get; set; }
    public string ProviderType { get; set; }
    public string Status { get; set; }
    public bool Executed { get; set; }
    public bool Success { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? FailedStepNumber { get; set; }
    public string? FailureMessage { get; set; }
    public int? RollbackOfWorkflowHistoryId { get; set; }
}
