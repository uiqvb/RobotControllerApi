namespace RobotControllerApi.BoundedContexts.Telemetry.Dtos;

public class LatestTelemetryResponse
{
    public int DeviceId { get; set; }
    public TelemetryReadingResponse? LatestReading { get; set; }
    public string ConnectionState { get; set; } = "Unknown";
    public string OperationalState { get; set; } = "Unknown";
    public string StatusMessage { get; set; } = "No telemetry received.";
    public bool IsStale { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}
