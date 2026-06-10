using Npgsql;
using RobotControllerApi.BoundedContexts.Telemetry.Models;
using RobotControllerApi.BoundedContexts.Telemetry.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class TelemetryReadingRepository : ITelemetryReadingDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public TelemetryReadingRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public List<TelemetryReading> GetTelemetryReadingsByDeviceId(int deviceId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 100)
    {
        var sql = @"SELECT id, deviceid, payloadjson, providertype, recordedatutc, createddate
                    FROM public.telemetryreading
                    WHERE deviceid = @deviceId";

        var parameters = new List<NpgsqlParameter>
        {
            new("deviceId", deviceId)
        };

        if (fromUtc.HasValue)
        {
            sql += " AND recordedatutc >= @fromUtc";
            parameters.Add(new NpgsqlParameter("fromUtc", fromUtc.Value));
        }

        if (toUtc.HasValue)
        {
            sql += " AND recordedatutc <= @toUtc";
            parameters.Add(new NpgsqlParameter("toUtc", toUtc.Value));
        }

        sql += " ORDER BY recordedatutc DESC, id DESC LIMIT @limit;";
        parameters.Add(new NpgsqlParameter("limit", limit));

        return _repo.ExecuteReader<TelemetryReading>(ConnectionString, sql, parameters.ToArray());
    }

    public TelemetryReading? GetLatestTelemetryReadingByDeviceId(int deviceId)
    {
        return _repo.ExecuteReader<TelemetryReading>(
            ConnectionString,
            @"SELECT id, deviceid, payloadjson, providertype, recordedatutc, createddate
              FROM public.telemetryreading
              WHERE deviceid = @deviceId
              ORDER BY recordedatutc DESC, id DESC
              LIMIT 1;",
            new NpgsqlParameter[]
            {
                new("deviceId", deviceId)
            })
            .SingleOrDefault();
    }

    public TelemetryReading InsertTelemetryReading(TelemetryReading newTelemetryReading)
    {
        return _repo.ExecuteReader<TelemetryReading>(
            ConnectionString,
            @"INSERT INTO public.telemetryreading
              (deviceid, payloadjson, providertype, recordedatutc, createddate)
              VALUES (@deviceId, @payloadJson::jsonb, @providerType, @recordedAtUtc, @createdDate)
              RETURNING id, deviceid, payloadjson, providertype, recordedatutc, createddate;",
            new NpgsqlParameter[]
            {
                new("deviceId", newTelemetryReading.DeviceId),
                new("payloadJson", newTelemetryReading.PayloadJson),
                new("providerType", newTelemetryReading.ProviderType),
                new("recordedAtUtc", newTelemetryReading.RecordedAtUtc),
                new("createdDate", newTelemetryReading.CreatedDate)
            })
            .Single();
    }
}
