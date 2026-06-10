using Npgsql;
using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class LiveControlCommandRepository : ILiveControlCommandDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public LiveControlCommandRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public LiveControlCommand? GetLiveControlCommandById(int id)
    {
        return _repo.ExecuteReader<LiveControlCommand>(ConnectionString,
            @"SELECT id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate
              FROM public.livecontrolcommand
              WHERE id = @id;",
            new NpgsqlParameter[] { new("id", id) }).SingleOrDefault();
    }

    public LiveControlCommand? GetLiveControlCommandByDeviceId(int deviceId)
    {
        return _repo.ExecuteReader<LiveControlCommand>(ConnectionString,
            @"SELECT id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate
              FROM public.livecontrolcommand
              WHERE deviceid = @deviceId
              ORDER BY modifieddate DESC, id DESC
              LIMIT 1;",
            new NpgsqlParameter[] { new("deviceId", deviceId) }).SingleOrDefault();
    }

    public LiveControlCommand InsertLiveControlCommand(LiveControlCommand newLiveControlCommand)
    {
        return _repo.ExecuteReader<LiveControlCommand>(ConnectionString,
            @"INSERT INTO public.livecontrolcommand
              (deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate)
              VALUES (@deviceId, @appUserId, @liveControlSessionId, @commandName, @payloadJson::jsonb, @sequenceNumber, @expiresAtUtc, @createdDate, @modifiedDate)
              RETURNING id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate;",
            Parameters(newLiveControlCommand)).Single();
    }

    public bool UpdateLiveControlCommand(int id, LiveControlCommand updatedLiveControlCommand)
    {
        var results = _repo.ExecuteReader<LiveControlCommand>(ConnectionString,
            @"UPDATE public.livecontrolcommand
              SET deviceid = @deviceId,
                  appuserid = @appUserId,
                  livecontrolsessionid = @liveControlSessionId,
                  commandname = @commandName,
                  payloadjson = @payloadJson::jsonb,
                  sequencenumber = @sequenceNumber,
                  expiresatutc = @expiresAtUtc,
                  modifieddate = @modifiedDate
              WHERE id = @id
              RETURNING id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate;",
            new[] { new NpgsqlParameter("id", id) }.Concat(Parameters(updatedLiveControlCommand)).ToArray());

        return results.Count > 0;
    }

    public bool DeleteLiveControlCommand(int id)
    {
        var results = _repo.ExecuteReader<LiveControlCommand>(ConnectionString,
            @"DELETE FROM public.livecontrolcommand
              WHERE id = @id
              RETURNING id, deviceid, appuserid, livecontrolsessionid, commandname, payloadjson, sequencenumber, expiresatutc, createddate, modifieddate;",
            new NpgsqlParameter[] { new("id", id) });

        return results.Count > 0;
    }

    private static NpgsqlParameter[] Parameters(LiveControlCommand model) => new NpgsqlParameter[]
    {
        new("deviceId", model.DeviceId),
        new("appUserId", (object?)model.AppUserId ?? DBNull.Value),
        new("liveControlSessionId", (object?)model.LiveControlSessionId ?? DBNull.Value),
        new("commandName", model.CommandName),
        new("payloadJson", model.PayloadJson),
        new("sequenceNumber", model.SequenceNumber),
        new("expiresAtUtc", model.ExpiresAtUtc),
        new("createdDate", model.CreatedDate),
        new("modifiedDate", model.ModifiedDate)
    };
}
