namespace RobotControllerApi.BoundedContexts.LiveControls.Dtos;

public class LiveControlSegmentResponse
{
    public int Id { get; set; }
    public int LiveControlSessionId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
}
