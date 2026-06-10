using Npgsql;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class RollbackRequestADO : IRollbackRequestDataAccess
{
    private readonly DbConfig _dbConfig;

    public RollbackRequestADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public List<RollbackRequest> GetRollbackRequests()
    {
        var results = new List<RollbackRequest>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate
              FROM public.rollbackrequest
              ORDER BY id;", conn);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            results.Add(MapRollbackRequest(dr));
        }

        return results;
    }

    public RollbackRequest? GetRollbackRequestById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate
              FROM public.rollbackrequest
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();

        return dr.Read() ? MapRollbackRequest(dr) : null;
    }

    public List<RollbackRequest> GetRollbackRequestsByDeviceId(int deviceId)
    {
        var results = new List<RollbackRequest>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate
              FROM public.rollbackrequest
              WHERE deviceid = @deviceId
              ORDER BY createddate DESC, id DESC;", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            results.Add(MapRollbackRequest(dr));
        }

        return results;
    }

    public RollbackRequest InsertRollbackRequest(RollbackRequest newRollbackRequest)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.rollbackrequest
              (deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate)
              VALUES (@deviceId, @requestedByAppUserId, @requestedByProvider, @targetType, @targetIdsJson::jsonb, @status, @generatedWorkflowId, @generatedJobId, @failureCode, @failureMessage, @createdDate, @modifiedDate)
              RETURNING id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate;", conn);

        AddParameters(cmd, newRollbackRequest);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapRollbackRequest(dr);
        }

        throw new InvalidOperationException("RollbackRequest insert failed.");
    }

    public bool UpdateRollbackRequest(int id, RollbackRequest updatedRollbackRequest)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE public.rollbackrequest
              SET deviceid = @deviceId,
                  requestedbyappuserid = @requestedByAppUserId,
                  requestedbyprovider = @requestedByProvider,
                  targettype = @targetType,
                  targetidsjson = @targetIdsJson::jsonb,
                  status = @status,
                  generatedworkflowid = @generatedWorkflowId,
                  generatedjobid = @generatedJobId,
                  failurecode = @failureCode,
                  failuremessage = @failureMessage,
                  modifieddate = @modifiedDate
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        AddParameters(cmd, updatedRollbackRequest);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeviceExistsAndActive(int deviceId)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT EXISTS (
                  SELECT 1
                  FROM public.device
                  WHERE id = @deviceId AND isactive = true
              );", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        return (bool)(cmd.ExecuteScalar() ?? false);
    }

    public bool HasGeneratedRollbackForTarget(string targetType, string targetIdsJson)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT EXISTS (
                  SELECT 1
                  FROM public.rollbackrequest
                  WHERE targettype = @targetType
                    AND targetidsjson = @targetIdsJson::jsonb
                    AND status = 'Generated'
              );", conn);

        cmd.Parameters.AddWithValue("targetType", targetType);
        cmd.Parameters.AddWithValue("targetIdsJson", targetIdsJson);

        return (bool)(cmd.ExecuteScalar() ?? false);
    }

    public CommandCatalogue? GetActiveCommandCatalogueById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              WHERE id = @id AND isactive = true;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();

        return dr.Read() ? MapCommandCatalogue(dr) : null;
    }

    public CommandCatalogue? GetActiveCommandCatalogueByName(string name)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              WHERE UPPER(name) = UPPER(@name) AND isactive = true
              ORDER BY id
              LIMIT 1;", conn);

        cmd.Parameters.AddWithValue("name", name);

        using var dr = cmd.ExecuteReader();

        return dr.Read() ? MapCommandCatalogue(dr) : null;
    }

    private static void AddParameters(NpgsqlCommand cmd, RollbackRequest model)
    {
        cmd.Parameters.AddWithValue("deviceId", model.DeviceId);
        cmd.Parameters.AddWithValue("requestedByAppUserId", (object?)model.RequestedByAppUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("requestedByProvider", model.RequestedByProvider);
        cmd.Parameters.AddWithValue("targetType", model.TargetType);
        cmd.Parameters.AddWithValue("targetIdsJson", model.TargetIdsJson);
        cmd.Parameters.AddWithValue("status", model.Status);
        cmd.Parameters.AddWithValue("generatedWorkflowId", (object?)model.GeneratedWorkflowId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("generatedJobId", (object?)model.GeneratedJobId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("failureCode", (object?)model.FailureCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("failureMessage", (object?)model.FailureMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("createdDate", model.CreatedDate);
        cmd.Parameters.AddWithValue("modifiedDate", model.ModifiedDate);
    }

    private static RollbackRequest MapRollbackRequest(NpgsqlDataReader dr)
    {
        return new RollbackRequest
        {
            Id = dr.GetInt32(0),
            DeviceId = dr.GetInt32(1),
            RequestedByAppUserId = dr.IsDBNull(2) ? null : dr.GetInt32(2),
            RequestedByProvider = dr.GetString(3),
            TargetType = dr.GetString(4),
            TargetIdsJson = dr.GetString(5),
            Status = dr.GetString(6),
            GeneratedWorkflowId = dr.IsDBNull(7) ? null : dr.GetInt32(7),
            GeneratedJobId = dr.IsDBNull(8) ? null : dr.GetInt32(8),
            FailureCode = dr.IsDBNull(9) ? null : dr.GetString(9),
            FailureMessage = dr.IsDBNull(10) ? null : dr.GetString(10),
            CreatedDate = dr.GetDateTime(11),
            ModifiedDate = dr.GetDateTime(12)
        };
    }

    private static CommandCatalogue MapCommandCatalogue(NpgsqlDataReader dr)
    {
        return new CommandCatalogue
        {
            Id = dr.GetInt32(0),
            Name = dr.GetString(1),
            Description = dr.IsDBNull(2) ? null : dr.GetString(2),
            ExecutionKind = dr.GetString(3),
            RollbackKind = dr.GetString(4),
            InverseCommandName = dr.IsDBNull(5) ? null : dr.GetString(5),
            RequiresDuration = dr.GetBoolean(6),
            IsActive = dr.GetBoolean(7),
            CreatedDate = dr.GetDateTime(8),
            ModifiedDate = dr.GetDateTime(9)
        };
    }
}
