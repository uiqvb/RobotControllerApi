namespace RobotControllerApi.BoundedContexts.Rollbacks.Models;

public class RollbackRequest
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; }
    public int? RequestedByAppUserId { get; set; }
    public string RequestedByProvider { get; set; } = "Api";
    public string TargetType { get; set; } = string.Empty;
    public string TargetIdsJson { get; set; } = "[]";
    public string Status { get; set; } = "Requested";
    public int? GeneratedWorkflowId { get; set; }
    public int? GeneratedJobId { get; set; }
    public string? FailureCode { get; set; } = null;
    public string? FailureMessage { get; set; } = null;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
