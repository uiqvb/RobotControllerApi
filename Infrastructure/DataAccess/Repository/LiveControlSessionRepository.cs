using Npgsql;
using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class LiveControlSessionRepository : ILiveControlSessionDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public LiveControlSessionRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public LiveControlSession? GetLiveControlSessionById(int id)
    {
        return _repo.ExecuteReader<LiveControlSession>(ConnectionString,
            SelectSql + " WHERE id = @id;",
            new NpgsqlParameter[] { new("id", id) }).SingleOrDefault();
    }

    public List<LiveControlSession> GetLiveControlSessionsByDeviceId(int deviceId)
    {
        return _repo.ExecuteReader<LiveControlSession>(ConnectionString,
            SelectSql + " WHERE deviceid = @deviceId ORDER BY startedatutc DESC, id DESC;",
            new NpgsqlParameter[] { new("deviceId", deviceId) });
    }

    public LiveControlSession? GetActiveLiveControlSessionByDeviceId(int deviceId)
    {
        return _repo.ExecuteReader<LiveControlSession>(ConnectionString,
            SelectSql + " WHERE deviceid = @deviceId AND status = 'Active' ORDER BY startedatutc DESC, id DESC LIMIT 1;",
            new NpgsqlParameter[] { new("deviceId", deviceId) }).SingleOrDefault();
    }

    public LiveControlSession InsertLiveControlSession(LiveControlSession newLiveControlSession)
    {
        return _repo.ExecuteReader<LiveControlSession>(ConnectionString,
            @"INSERT INTO public.livecontrolsession
              (deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate)
              VALUES (@deviceId, @appUserId, @status, @stopReason, @isRollback, @rollbackOfLiveControlSessionId, @startedAtUtc, @endedAtUtc, @createdDate, @modifiedDate)
              RETURNING id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate;",
            Parameters(newLiveControlSession)).Single();
    }

    public bool UpdateLiveControlSession(int id, LiveControlSession updatedLiveControlSession)
    {
        var results = _repo.ExecuteReader<LiveControlSession>(ConnectionString,
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
              WHERE id = @id
              RETURNING id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate;",
            new[] { new NpgsqlParameter("id", id) }.Concat(Parameters(updatedLiveControlSession)).ToArray());

        return results.Count > 0;
    }

    public bool DeleteLiveControlSession(int id)
    {
        var results = _repo.ExecuteReader<LiveControlSession>(ConnectionString,
            @"DELETE FROM public.livecontrolsession
              WHERE id = @id
              RETURNING id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate;",
            new NpgsqlParameter[] { new("id", id) });

        return results.Count > 0;
    }

    private const string SelectSql = @"SELECT id, deviceid, appuserid, status, stopreason, isrollback, rollbackoflivecontrolsessionid, startedatutc, endedatutc, createddate, modifieddate
                                       FROM public.livecontrolsession";

    private static NpgsqlParameter[] Parameters(LiveControlSession model) => new NpgsqlParameter[]
    {
        new("deviceId", model.DeviceId),
        new("appUserId", model.AppUserId),
        new("status", model.Status),
        new("stopReason", (object?)model.StopReason ?? DBNull.Value),
        new("isRollback", model.IsRollback),
        new("rollbackOfLiveControlSessionId", (object?)model.RollbackOfLiveControlSessionId ?? DBNull.Value),
        new("startedAtUtc", model.StartedAtUtc),
        new("endedAtUtc", (object?)model.EndedAtUtc ?? DBNull.Value),
        new("createdDate", model.CreatedDate),
        new("modifiedDate", model.ModifiedDate)
    };
}
