using Npgsql;
using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class LiveControlSegmentRepository : ILiveControlSegmentDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public LiveControlSegmentRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public LiveControlSegment? GetLiveControlSegmentById(int id)
    {
        return _repo.ExecuteReader<LiveControlSegment>(ConnectionString,
            SelectSql + " WHERE id = @id;",
            new NpgsqlParameter[] { new("id", id) }).SingleOrDefault();
    }

    public List<LiveControlSegment> GetLiveControlSegmentsBySessionId(int liveControlSessionId)
    {
        return _repo.ExecuteReader<LiveControlSegment>(ConnectionString,
            SelectSql + " WHERE livecontrolsessionid = @liveControlSessionId ORDER BY startedatutc, id;",
            new NpgsqlParameter[] { new("liveControlSessionId", liveControlSessionId) });
    }

    public LiveControlSegment InsertLiveControlSegment(LiveControlSegment newLiveControlSegment)
    {
        return _repo.ExecuteReader<LiveControlSegment>(ConnectionString,
            @"INSERT INTO public.livecontrolsegment
              (livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate)
              VALUES (@liveControlSessionId, @commandName, @payloadJson::jsonb, @durationMs, @success, @failureCode, @failureMessage, @startedAtUtc, @completedAtUtc, @createdDate)
              RETURNING id, livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate;",
            Parameters(newLiveControlSegment)).Single();
    }

    public bool DeleteLiveControlSegment(int id)
    {
        var results = _repo.ExecuteReader<LiveControlSegment>(ConnectionString,
            @"DELETE FROM public.livecontrolsegment
              WHERE id = @id
              RETURNING id, livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate;",
            new NpgsqlParameter[] { new("id", id) });

        return results.Count > 0;
    }

    private const string SelectSql = @"SELECT id, livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate
                                       FROM public.livecontrolsegment";

    private static NpgsqlParameter[] Parameters(LiveControlSegment model) => new NpgsqlParameter[]
    {
        new("liveControlSessionId", model.LiveControlSessionId),
        new("commandName", model.CommandName),
        new("payloadJson", model.PayloadJson),
        new("durationMs", model.DurationMs),
        new("success", model.Success),
        new("failureCode", (object?)model.FailureCode ?? DBNull.Value),
        new("failureMessage", (object?)model.FailureMessage ?? DBNull.Value),
        new("startedAtUtc", model.StartedAtUtc),
        new("completedAtUtc", model.CompletedAtUtc),
        new("createdDate", model.CreatedDate)
    };
}
