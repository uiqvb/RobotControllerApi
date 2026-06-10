namespace RobotControllerApi.BoundedContexts.LiveControls.Dtos;

public class StopLiveControlSessionRequest
{
    public string StopReason { get; set; } = "UserReleased";
}
