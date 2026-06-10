namespace RobotControllerApi.BoundedContexts.Telemetry.Models;

public class TelemetryReading
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ProviderType { get; set; } = "Poll";
    public DateTime RecordedAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
}
