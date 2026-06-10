using System.Text.Json;

namespace RobotControllerApi.BoundedContexts.Telemetry.Dtos;

public class CreateTelemetryReadingRequest
{
    public JsonElement Payload { get; set; }
    public string ProviderType { get; set; } = "Poll";
    public DateTime? RecordedAtUtc { get; set; }
}
