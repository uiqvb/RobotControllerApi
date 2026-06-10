using Npgsql;
using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class LiveControlSegmentADO : ILiveControlSegmentDataAccess
{
    private readonly DbConfig _dbConfig;

    public LiveControlSegmentADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public LiveControlSegment? GetLiveControlSegmentById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate
              FROM public.livecontrolsegment
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? MapLiveControlSegment(dr) : null;
    }

    public List<LiveControlSegment> GetLiveControlSegmentsBySessionId(int liveControlSessionId)
    {
        var results = new List<LiveControlSegment>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate
              FROM public.livecontrolsegment
              WHERE livecontrolsessionid = @liveControlSessionId
              ORDER BY startedatutc, id;", conn);

        cmd.Parameters.AddWithValue("liveControlSessionId", liveControlSessionId);

        using var dr = cmd.ExecuteReader();
        while (dr.Read()) results.Add(MapLiveControlSegment(dr));
        return results;
    }

    public LiveControlSegment InsertLiveControlSegment(LiveControlSegment newLiveControlSegment)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.livecontrolsegment
              (livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate)
              VALUES (@liveControlSessionId, @commandName, @payloadJson::jsonb, @durationMs, @success, @failureCode, @failureMessage, @startedAtUtc, @completedAtUtc, @createdDate)
              RETURNING id, livecontrolsessionid, commandname, payloadjson, durationms, success, failurecode, failuremessage, startedatutc, completedatutc, createddate;", conn);

        AddParameters(cmd, newLiveControlSegment);

        using var dr = cmd.ExecuteReader();
        if (dr.Read()) return MapLiveControlSegment(dr);
        throw new InvalidOperationException("LiveControlSegment insert failed.");
    }

    public bool DeleteLiveControlSegment(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(@"DELETE FROM public.livecontrolsegment WHERE id = @id;", conn);
        cmd.Parameters.AddWithValue("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void AddParameters(NpgsqlCommand cmd, LiveControlSegment model)
    {
        cmd.Parameters.AddWithValue("liveControlSessionId", model.LiveControlSessionId);
        cmd.Parameters.AddWithValue("commandName", model.CommandName);
        cmd.Parameters.AddWithValue("payloadJson", model.PayloadJson);
        cmd.Parameters.AddWithValue("durationMs", model.DurationMs);
        cmd.Parameters.AddWithValue("success", model.Success);
        cmd.Parameters.AddWithValue("failureCode", (object?)model.FailureCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("failureMessage", (object?)model.FailureMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("startedAtUtc", model.StartedAtUtc);
        cmd.Parameters.AddWithValue("completedAtUtc", model.CompletedAtUtc);
        cmd.Parameters.AddWithValue("createdDate", model.CreatedDate);
    }

    private static LiveControlSegment MapLiveControlSegment(NpgsqlDataReader dr)
    {
        return new LiveControlSegment
        {
            Id = dr.GetInt32(0),
            LiveControlSessionId = dr.GetInt32(1),
            CommandName = dr.GetString(2),
            PayloadJson = dr.GetString(3),
            DurationMs = dr.GetInt32(4),
            Success = dr.GetBoolean(5),
            FailureCode = dr.IsDBNull(6) ? null : dr.GetString(6),
            FailureMessage = dr.IsDBNull(7) ? null : dr.GetString(7),
            StartedAtUtc = dr.GetDateTime(8),
            CompletedAtUtc = dr.GetDateTime(9),
            CreatedDate = dr.GetDateTime(10)
        };
    }
}
