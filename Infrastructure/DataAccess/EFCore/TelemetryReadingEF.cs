using RobotControllerApi.BoundedContexts.Telemetry.Models;
using RobotControllerApi.BoundedContexts.Telemetry.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class TelemetryReadingEF : ITelemetryReadingDataAccess
{
    private readonly RobotContext _context;

    public TelemetryReadingEF(RobotContext context)
    {
        _context = context;
    }

    public List<TelemetryReading> GetTelemetryReadingsByDeviceId(int deviceId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 100)
    {
        var query = _context.TelemetryReadings
            .Where(x => x.DeviceId == deviceId);

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc <= toUtc.Value);
        }

        return query
            .OrderByDescending(x => x.RecordedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToList();
    }

    public TelemetryReading? GetLatestTelemetryReadingByDeviceId(int deviceId)
    {
        return _context.TelemetryReadings
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.RecordedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    public TelemetryReading InsertTelemetryReading(TelemetryReading newTelemetryReading)
    {
        _context.TelemetryReadings.Add(newTelemetryReading);
        _context.SaveChanges();
        return newTelemetryReading;
    }
}
