namespace RobotControllerApi.BoundedContexts.LiveControls.Dtos;

public class CreateLiveControlSegmentRequest
{
    public string CommandName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public int DurationMs { get; set; }
    public bool Success { get; set; } = true;
    public string? FailureCode { get; set; } = null;
    public string? FailureMessage { get; set; } = null;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
