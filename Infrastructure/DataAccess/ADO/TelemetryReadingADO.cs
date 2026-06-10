using Npgsql;
using RobotControllerApi.BoundedContexts.Telemetry.Models;
using RobotControllerApi.BoundedContexts.Telemetry.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class TelemetryReadingADO : ITelemetryReadingDataAccess
{
    private readonly DbConfig _dbConfig;

    public TelemetryReadingADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public List<TelemetryReading> GetTelemetryReadingsByDeviceId(int deviceId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 100)
    {
        var results = new List<TelemetryReading>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        var sql = @"SELECT id, deviceid, payloadjson, providertype, recordedatutc, createddate
                    FROM public.telemetryreading
                    WHERE deviceid = @deviceId";

        if (fromUtc.HasValue)
        {
            sql += " AND recordedatutc >= @fromUtc";
        }

        if (toUtc.HasValue)
        {
            sql += " AND recordedatutc <= @toUtc";
        }

        sql += " ORDER BY recordedatutc DESC, id DESC LIMIT @limit;";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("deviceId", deviceId);
        cmd.Parameters.AddWithValue("limit", limit);

        if (fromUtc.HasValue)
        {
            cmd.Parameters.AddWithValue("fromUtc", fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            cmd.Parameters.AddWithValue("toUtc", toUtc.Value);
        }

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            results.Add(MapTelemetryReading(dr));
        }

        return results;
    }

    public TelemetryReading? GetLatestTelemetryReadingByDeviceId(int deviceId)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, payloadjson, providertype, recordedatutc, createddate
              FROM public.telemetryreading
              WHERE deviceid = @deviceId
              ORDER BY recordedatutc DESC, id DESC
              LIMIT 1;", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        using var dr = cmd.ExecuteReader();

        return dr.Read() ? MapTelemetryReading(dr) : null;
    }

    public TelemetryReading InsertTelemetryReading(TelemetryReading newTelemetryReading)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.telemetryreading
              (deviceid, payloadjson, providertype, recordedatutc, createddate)
              VALUES (@deviceId, @payloadJson::jsonb, @providerType, @recordedAtUtc, @createdDate)
              RETURNING id, deviceid, payloadjson, providertype, recordedatutc, createddate;", conn);

        AddParameters(cmd, newTelemetryReading);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapTelemetryReading(dr);
        }

        throw new InvalidOperationException("TelemetryReading insert failed.");
    }

    private static void AddParameters(NpgsqlCommand cmd, TelemetryReading model)
    {
        cmd.Parameters.AddWithValue("deviceId", model.DeviceId);
        cmd.Parameters.AddWithValue("payloadJson", model.PayloadJson);
        cmd.Parameters.AddWithValue("providerType", model.ProviderType);
        cmd.Parameters.AddWithValue("recordedAtUtc", model.RecordedAtUtc);
        cmd.Parameters.AddWithValue("createdDate", model.CreatedDate);
    }

    private static TelemetryReading MapTelemetryReading(NpgsqlDataReader dr)
    {
        return new TelemetryReading
        {
            Id = dr.GetInt32(0),
            DeviceId = dr.GetInt32(1),
            PayloadJson = dr.GetString(2),
            ProviderType = dr.GetString(3),
            RecordedAtUtc = dr.GetDateTime(4),
            CreatedDate = dr.GetDateTime(5)
        };
    }
}
