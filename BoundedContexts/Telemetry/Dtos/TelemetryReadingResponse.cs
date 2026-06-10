namespace RobotControllerApi.BoundedContexts.Telemetry.Dtos;

public class TelemetryReadingResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ProviderType { get; set; } = "Poll";
    public DateTime RecordedAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
}
