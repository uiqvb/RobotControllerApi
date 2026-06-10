using Npgsql;
using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class LiveControlCommandADO : ILiveControlCommandDataAccess
{
    private readonly DbConfig _dbConfig;

    public LiveControlCommandADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public LiveControlCommand? GetLiveControlCommandById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate
              FROM public.livecontrolcommand
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? MapLiveControlCommand(dr) : null;
    }

    public LiveControlCommand? GetLiveControlCommandByDeviceId(int deviceId)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate
              FROM public.livecontrolcommand
              WHERE deviceid = @deviceId
              ORDER BY modifieddate DESC, id DESC
              LIMIT 1;", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? MapLiveControlCommand(dr) : null;
    }

    public LiveControlCommand InsertLiveControlCommand(LiveControlCommand newLiveControlCommand)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.livecontrolcommand
              (deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate)
              VALUES (@deviceId, @appUserId, @liveControlSessionId, @commandName, @payloadJson::jsonb, @sequenceNumber, @expiresAtUtc, @createdDate, @modifiedDate)
              RETURNING id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate;", conn);

        AddParameters(cmd, newLiveControlCommand);

        using var dr = cmd.ExecuteReader();
        if (dr.Read()) return MapLiveControlCommand(dr);
        throw new InvalidOperationException("LiveControlCommand insert failed.");
    }

    public bool UpdateLiveControlCommand(int id, LiveControlCommand updatedLiveControlCommand)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE public.livecontrolcommand
              SET deviceid = @deviceId,
                  appuserid = @appUserId,
                  livecontrolsessionid = @liveControlSessionId,
                  commandname = @commandName,
                  payloadjson = @payloadJson::jsonb,
                  sequencenumber = @sequenceNumber,
                  expiresatutc = @expiresAtUtc,
                  modifieddate = @modifiedDate
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        AddParameters(cmd, updatedLiveControlCommand);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteLiveControlCommand(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"DELETE FROM public.livecontrolcommand
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void AddParameters(NpgsqlCommand cmd, LiveControlCommand model)
    {
        cmd.Parameters.AddWithValue("deviceId", model.DeviceId);
        cmd.Parameters.AddWithValue("appUserId", (object?)model.AppUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("liveControlSessionId", (object?)model.LiveControlSessionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("commandName", model.CommandName);
        cmd.Parameters.AddWithValue("payloadJson", model.PayloadJson);
        cmd.Parameters.AddWithValue("sequenceNumber", model.SequenceNumber);
        cmd.Parameters.AddWithValue("expiresAtUtc", model.ExpiresAtUtc);
        cmd.Parameters.AddWithValue("createdDate", model.CreatedDate);
        cmd.Parameters.AddWithValue("modifiedDate", model.ModifiedDate);
    }

    private static LiveControlCommand MapLiveControlCommand(NpgsqlDataReader dr)
    {
        return new LiveControlCommand
        {
            Id = dr.GetInt32(0),
            DeviceId = dr.GetInt32(1),
            AppUserId = dr.IsDBNull(2) ? null : dr.GetInt32(2),
            LiveControlSessionId = dr.IsDBNull(3) ? null : dr.GetInt32(3),
            CommandName = dr.GetString(4),
            PayloadJson = dr.GetString(5),
            SequenceNumber = dr.GetInt32(6),
            ExpiresAtUtc = dr.GetDateTime(7),
            CreatedDate = dr.GetDateTime(8),
            ModifiedDate = dr.GetDateTime(9)
        };
    }
}
