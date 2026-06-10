namespace RobotControllerApi.BoundedContexts.LiveControls.Models;

public class LiveControlSession
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; }
    public int AppUserId { get; set; }
    public string Status { get; set; } = "Active";
    public string? StopReason { get; set; } = null;
    public bool IsRollback { get; set; } = false;
    public int? RollbackOfLiveControlSessionId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
