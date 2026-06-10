namespace RobotControllerApi.BoundedContexts.Rollbacks.Dtos;

public class CreateRollbackFromJobHistoryRequest
{
    public int DeviceId { get; set; }
    public int? RequestedByAppUserId { get; set; }
    public string RequestedByProvider { get; set; } = "Api";
    public List<int> JobHistoryIds { get; set; } = new();
    public bool AllowDuplicate { get; set; } = false;
}
