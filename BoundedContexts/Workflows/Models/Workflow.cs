namespace RobotControllerApi.BoundedContexts.Workflows.Models;

public class Workflow
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;
    public string SchemaVersion { get; set; } = "1.0";
    public string ExecutionMode { get; set; } = "BestEffort";
    public string ProviderType { get; set; } = "Api";
    public string Status { get; set; } = "Queued";
    public int? RequestedByAppUserId { get; set; }
    public int? ClaimedByDeviceCredentialId { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public bool IsRollback { get; set; } = false;
    public int? RollbackOfWorkflowHistoryId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
