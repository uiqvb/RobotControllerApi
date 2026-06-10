namespace RobotControllerApi.BoundedContexts.LiveControls.Dtos;

public class LiveControlSessionResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int AppUserId { get; set; }
    public string Status { get; set; } = "Active";
    public string? StopReason { get; set; }
    public bool IsRollback { get; set; }
    public int? RollbackOfLiveControlSessionId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
