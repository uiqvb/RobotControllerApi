namespace RobotControllerApi.BoundedContexts.LiveControls.Models;

public class LiveControlSegment
{
    public int Id { get; set; } = 0;
    public int LiveControlSessionId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public int DurationMs { get; set; }
    public bool Success { get; set; } = true;
    public string? FailureCode { get; set; } = null;
    public string? FailureMessage { get; set; } = null;
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
}
