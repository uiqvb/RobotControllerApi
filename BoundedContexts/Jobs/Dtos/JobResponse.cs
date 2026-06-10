namespace RobotControllerApi.BoundedContexts.Jobs.Dtos;

public class JobResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int? WorkflowId { get; set; }
    public int? StepNumber { get; set; }
    public int CommandCatalogueId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ProviderType { get; set; }
    public string Status { get; set; }
    public int? RequestedByAppUserId { get; set; }
    public int? ClaimedByDeviceCredentialId { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public bool IsRollback { get; set; }
    public int? RollbackOfJobHistoryId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
