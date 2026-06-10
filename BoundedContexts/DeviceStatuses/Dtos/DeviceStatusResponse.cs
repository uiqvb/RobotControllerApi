namespace RobotControllerApi.BoundedContexts.DeviceStatuses.Dtos;

public class DeviceStatusResponse
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; } = 0;
    public string ConnectionState { get; set; } = "Unknown";
    public string OperationalState { get; set; } = "Unknown";
    public DateTime? LastSeenAtUtc { get; set; } = null;
    public DateTime? LastHeartbeatAtUtc { get; set; } = null;
    public int? PoseMapId { get; set; } = null;
    public int? GridX { get; set; } = null;
    public int? GridY { get; set; } = null;
    public string? Facing { get; set; } = null;
    public bool IsGridAligned { get; set; } = false;
    public bool IsGridPoseTrusted { get; set; } = false;
    public double? PoseConfidence { get; set; } = null;
    public double? EstimatedXcm { get; set; } = null;
    public double? EstimatedYcm { get; set; } = null;
    public double? EstimatedHeadingDegrees { get; set; } = null;
    public bool? IsInsideMap { get; set; } = null;
    public string? StatusMessage { get; set; } = null;
    public string? LastErrorCode { get; set; } = null;
    public string? LastErrorMessage { get; set; } = null;
    public DateTime CreatedDate { get; set; } = default;
    public DateTime ModifiedDate { get; set; } = default;
}
