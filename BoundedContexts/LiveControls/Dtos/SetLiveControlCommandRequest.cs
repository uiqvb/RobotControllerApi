namespace RobotControllerApi.BoundedContexts.LiveControls.Dtos;

public class SetLiveControlCommandRequest
{
    public int? LiveControlSessionId { get; set; }
    public string CommandName { get; set; } = "STOP";
    public string PayloadJson { get; set; } = "{}";
    public int SequenceNumber { get; set; }
    public int ExpiresInMs { get; set; } = 500;
}
