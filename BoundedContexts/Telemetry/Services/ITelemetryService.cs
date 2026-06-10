using RobotControllerApi.BoundedContexts.Telemetry.Dtos;

namespace RobotControllerApi.BoundedContexts.Telemetry.Services;

public interface ITelemetryService
{
    TelemetryReadingResponse CreateTelemetryReading(int deviceId, CreateTelemetryReadingRequest request);
    TelemetryReadingResponse? GetLatestTelemetryReadingByDeviceId(int deviceId);
    List<TelemetryReadingResponse> GetTelemetryReadingsByDeviceId(int deviceId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 100);
    LatestTelemetryResponse GetLatestTelemetrySummaryByDeviceId(int deviceId);
}
