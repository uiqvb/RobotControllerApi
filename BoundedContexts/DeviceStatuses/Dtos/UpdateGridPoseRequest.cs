namespace RobotControllerApi.BoundedContexts.DeviceStatuses.Dtos;

public class UpdateGridPoseRequest
{
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
}
