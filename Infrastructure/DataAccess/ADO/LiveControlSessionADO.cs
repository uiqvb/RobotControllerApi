using Npgsql;
using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class LiveControlSessionADO : ILiveControlSessionDataAccess
{
    private readonly DbConfig _dbConfig;

    public LiveControlSessionADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public LiveControlSession? GetLiveControlSessionById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate
              FROM public.livecontrolsession
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? MapLiveControlSession(dr) : null;
    }

    public List<LiveControlSession> GetLiveControlSessionsByDeviceId(int deviceId)
    {
        var results = new List<LiveControlSession>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate
              FROM public.livecontrolsession
              WHERE deviceid = @deviceId
              ORDER BY startedatutc DESC, id DESC;", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        using var dr = cmd.ExecuteReader();
        while (dr.Read()) results.Add(MapLiveControlSession(dr));
        return results;
    }

    public LiveControlSession? GetActiveLiveControlSessionByDeviceId(int deviceId)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate
              FROM public.livecontrolsession
              WHERE deviceid = @deviceId AND status = 'Active'
              ORDER BY startedatutc DESC, id DESC
              LIMIT 1;", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? MapLiveControlSession(dr) : null;
    }

    public LiveControlSession InsertLiveControlSession(LiveControlSession newLiveControlSession)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.livecontrolsession
              (deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate)
              VALUES (@deviceId, @appUserId, @status, @stopReason, @isRollback, @rollbackOfLiveControlSessionId, @startedAtUtc, @endedAtUtc, @createdDate, @modifiedDate)
              RETURNING id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate;", conn);

        AddParameters(cmd, newLiveControlSession);

        using var dr = cmd.ExecuteReader();
        if (dr.Read()) return MapLiveControlSession(dr);
        throw new InvalidOperationException("LiveControlSession insert failed.");
    }

    public bool UpdateLiveControlSession(int id, LiveControlSession updatedLiveControlSession)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE public.livecontrolsession
              SET deviceid = @deviceId,
                  appuserid = @appUserId,
                  status = @status,
                  stopreason = @stopReason,
                  isrollback = @isRollback,
                  rollbackoflivecontrolsessionid = @rollbackOfLiveControlSessionId,
                  startedatutc = @startedAtUtc,
                  endedatutc = @endedAtUtc,
                  modifieddate = @modifiedDate
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        AddParameters(cmd, updatedLiveControlSession);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteLiveControlSession(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(@"DELETE FROM public.livecontrolsession WHERE id = @id;", conn);
        cmd.Parameters.AddWithValue("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void AddParameters(NpgsqlCommand cmd, LiveControlSession model)
    {
        cmd.Parameters.AddWithValue("deviceId", model.DeviceId);
        cmd.Parameters.AddWithValue("appUserId", model.AppUserId);
        cmd.Parameters.AddWithValue("status", model.Status);
        cmd.Parameters.AddWithValue("stopReason", (object?)model.StopReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("isRollback", model.IsRollback);
        cmd.Parameters.AddWithValue("rollbackOfLiveControlSessionId", (object?)model.RollbackOfLiveControlSessionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("startedAtUtc", model.StartedAtUtc);
        cmd.Parameters.AddWithValue("endedAtUtc", (object?)model.EndedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("createdDate", model.CreatedDate);
        cmd.Parameters.AddWithValue("modifiedDate", model.ModifiedDate);
    }

    private static LiveControlSession MapLiveControlSession(NpgsqlDataReader dr)
    {
        return new LiveControlSession
        {
            Id = dr.GetInt32(0),
            DeviceId = dr.GetInt32(1),
            AppUserId = dr.GetInt32(2),
            Status = dr.GetString(3),
            StopReason = dr.IsDBNull(4) ? null : dr.GetString(4),
            IsRollback = dr.GetBoolean(5),
            RollbackOfLiveControlSessionId = dr.IsDBNull(6) ? null : dr.GetInt32(6),
            StartedAtUtc = dr.GetDateTime(7),
            EndedAtUtc = dr.IsDBNull(8) ? null : dr.GetDateTime(8),
            CreatedDate = dr.GetDateTime(9),
            ModifiedDate = dr.GetDateTime(10)
        };
    }
}
