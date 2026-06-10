namespace RobotControllerApi.BoundedContexts.Rollbacks.Dtos;

public class CreateRollbackFromWorkflowHistoryRequest
{
    public int DeviceId { get; set; }
    public int? RequestedByAppUserId { get; set; }
    public string RequestedByProvider { get; set; } = "Api";
    public List<int> WorkflowHistoryIds { get; set; } = new();
    public bool AllowDuplicate { get; set; } = false;
}
