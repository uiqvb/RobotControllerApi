namespace RobotControllerApi.BoundedContexts.LiveControls.Dtos;

public class StartLiveControlSessionRequest
{
    public bool IsRollback { get; set; } = false;
    public int? RollbackOfLiveControlSessionId { get; set; }
}
