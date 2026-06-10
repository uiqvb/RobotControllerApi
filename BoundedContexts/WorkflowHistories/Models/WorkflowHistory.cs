namespace RobotControllerApi.BoundedContexts.WorkflowHistories.Models;

public class WorkflowHistory
{
    public int Id { get; set; } = 0;
    public int? WorkflowId { get; set; }
    public int DeviceId { get; set; }
    public string ProviderType { get; set; } = "Api";
    public string Status { get; set; } = "Completed";
    public bool Executed { get; set; }
    public bool Success { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? FailedStepNumber { get; set; }
    public string? FailureMessage { get; set; } = null;
    public int? RollbackOfWorkflowHistoryId { get; set; }
    public DateTime CreatedDate { get; set; }
}
