namespace RobotControllerApi.BoundedContexts.Workflows.Dtos;

public class WorkflowResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string SchemaVersion { get; set; }
    public string ExecutionMode { get; set; }
    public string ProviderType { get; set; }
    public string Status { get; set; }
    public int? RequestedByAppUserId { get; set; }
    public int? ClaimedByDeviceCredentialId { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public bool IsRollback { get; set; }
    public int? RollbackOfWorkflowHistoryId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
