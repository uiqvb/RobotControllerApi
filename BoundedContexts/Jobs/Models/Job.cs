namespace RobotControllerApi.BoundedContexts.Jobs.Models;

public class Job
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; }
    public int? WorkflowId { get; set; }
    public int? StepNumber { get; set; }
    public int CommandCatalogueId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ProviderType { get; set; } = "Api";
    public string Status { get; set; } = "Queued";
    public int? RequestedByAppUserId { get; set; }
    public int? ClaimedByDeviceCredentialId { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public bool IsRollback { get; set; } = false;
    public int? RollbackOfJobHistoryId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
