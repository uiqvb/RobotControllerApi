namespace RobotControllerApi.BoundedContexts.Workflows.Dtos;

public class CreateWorkflowRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;
    public string SchemaVersion { get; set; } = "1.0";
    public string ExecutionMode { get; set; } = "BestEffort";
    public string ProviderType { get; set; } = "Api";
    public int? RequestedByAppUserId { get; set; }
    public bool IsRollback { get; set; } = false;
    public int? RollbackOfWorkflowHistoryId { get; set; }
    public List<CreateWorkflowStepRequest> Steps { get; set; } = new();
}
