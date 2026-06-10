namespace RobotControllerApi.BoundedContexts.LiveControls.Models;

public class LiveControlCommand
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; }
    public int? AppUserId { get; set; }
    public int? LiveControlSessionId { get; set; }
    public string CommandName { get; set; } = "STOP";
    public string PayloadJson { get; set; } = "{}";
    public int SequenceNumber { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
