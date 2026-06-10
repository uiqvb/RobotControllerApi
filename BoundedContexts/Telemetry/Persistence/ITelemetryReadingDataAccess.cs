using RobotControllerApi.BoundedContexts.Telemetry.Models;

namespace RobotControllerApi.BoundedContexts.Telemetry.Persistence;

public interface ITelemetryReadingDataAccess
{
    List<TelemetryReading> GetTelemetryReadingsByDeviceId(int deviceId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 100);
    TelemetryReading? GetLatestTelemetryReadingByDeviceId(int deviceId);
    TelemetryReading InsertTelemetryReading(TelemetryReading newTelemetryReading);
}
