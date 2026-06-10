namespace RobotControllerApi.BoundedContexts.Rollbacks.Dtos;

public class RollbackRequestResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int? RequestedByAppUserId { get; set; }
    public string RequestedByProvider { get; set; }
    public string TargetType { get; set; }
    public string TargetIdsJson { get; set; }
    public string Status { get; set; }
    public int? GeneratedWorkflowId { get; set; }
    public int? GeneratedJobId { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
