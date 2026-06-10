using Npgsql;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class RollbackRequestRepository : IRollbackRequestDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public RollbackRequestRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public List<RollbackRequest> GetRollbackRequests()
    {
        return _repo.ExecuteReader<RollbackRequest>(
            ConnectionString,
            @"SELECT id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate
              FROM public.rollbackrequest
              ORDER BY id;");
    }

    public RollbackRequest? GetRollbackRequestById(int id)
    {
        return _repo.ExecuteReader<RollbackRequest>(
            ConnectionString,
            @"SELECT id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate
              FROM public.rollbackrequest
              WHERE id = @id;",
            new[]
            {
                new NpgsqlParameter("id", id)
            })
            .SingleOrDefault();
    }

    public List<RollbackRequest> GetRollbackRequestsByDeviceId(int deviceId)
    {
        return _repo.ExecuteReader<RollbackRequest>(
            ConnectionString,
            @"SELECT id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate
              FROM public.rollbackrequest
              WHERE deviceid = @deviceId
              ORDER BY createddate DESC, id DESC;",
            new[]
            {
                new NpgsqlParameter("deviceId", deviceId)
            });
    }

    public RollbackRequest InsertRollbackRequest(RollbackRequest newRollbackRequest)
    {
        return _repo.ExecuteReader<RollbackRequest>(
            ConnectionString,
            @"INSERT INTO public.rollbackrequest
              (deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate)
              VALUES (@deviceId, @requestedByAppUserId, @requestedByProvider, @targetType, @targetIdsJson::jsonb, @status, @generatedWorkflowId, @generatedJobId, @failureCode, @failureMessage, @createdDate, @modifiedDate)
              RETURNING id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate;",
            CreateParameters(newRollbackRequest))
            .Single();
    }

    public bool UpdateRollbackRequest(int id, RollbackRequest updatedRollbackRequest)
    {
        var parameters = new List<NpgsqlParameter>
        {
            new("id", id)
        };
        parameters.AddRange(CreateParameters(updatedRollbackRequest));

        var result = _repo.ExecuteReader<RollbackRequest>(
            ConnectionString,
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
              WHERE id = @id
              RETURNING id, deviceid, requestedbyappuserid, requestedbyprovider, targettype, targetidsjson, status, generatedworkflowid, generatedjobid, failurecode, failuremessage, createddate, modifieddate;",
            parameters.ToArray());

        return result.Any();
    }

    public bool DeviceExistsAndActive(int deviceId)
    {
        return ReadBool(
            @"SELECT EXISTS (
                  SELECT 1
                  FROM public.device
                  WHERE id = @deviceId AND isactive = true
              ) AS value;",
            new[]
            {
                new NpgsqlParameter("deviceId", deviceId)
            });
    }

    public bool HasGeneratedRollbackForTarget(string targetType, string targetIdsJson)
    {
        return ReadBool(
            @"SELECT EXISTS (
                  SELECT 1
                  FROM public.rollbackrequest
                  WHERE targettype = @targetType
                    AND targetidsjson = @targetIdsJson::jsonb
                    AND status = 'Generated'
              ) AS value;",
            new[]
            {
                new NpgsqlParameter("targetType", targetType),
                new NpgsqlParameter("targetIdsJson", targetIdsJson)
            });
    }

    public CommandCatalogue? GetActiveCommandCatalogueById(int id)
    {
        return _repo.ExecuteReader<CommandCatalogue>(
            ConnectionString,
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              WHERE id = @id AND isactive = true;",
            new[]
            {
                new NpgsqlParameter("id", id)
            })
            .SingleOrDefault();
    }

    public CommandCatalogue? GetActiveCommandCatalogueByName(string name)
    {
        return _repo.ExecuteReader<CommandCatalogue>(
            ConnectionString,
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              WHERE UPPER(name) = UPPER(@name) AND isactive = true
              ORDER BY id
              LIMIT 1;",
            new[]
            {
                new NpgsqlParameter("name", name)
            })
            .SingleOrDefault();
    }

    private static NpgsqlParameter[] CreateParameters(RollbackRequest model)
    {
        return new[]
        {
            new NpgsqlParameter("deviceId", model.DeviceId),
            new NpgsqlParameter("requestedByAppUserId", (object?)model.RequestedByAppUserId ?? DBNull.Value),
            new NpgsqlParameter("requestedByProvider", model.RequestedByProvider),
            new NpgsqlParameter("targetType", model.TargetType),
            new NpgsqlParameter("targetIdsJson", model.TargetIdsJson),
            new NpgsqlParameter("status", model.Status),
            new NpgsqlParameter("generatedWorkflowId", (object?)model.GeneratedWorkflowId ?? DBNull.Value),
            new NpgsqlParameter("generatedJobId", (object?)model.GeneratedJobId ?? DBNull.Value),
            new NpgsqlParameter("failureCode", (object?)model.FailureCode ?? DBNull.Value),
            new NpgsqlParameter("failureMessage", (object?)model.FailureMessage ?? DBNull.Value),
            new NpgsqlParameter("createdDate", model.CreatedDate),
            new NpgsqlParameter("modifiedDate", model.ModifiedDate)
        };
    }

    private bool ReadBool(string sql, NpgsqlParameter[] parameters)
    {
        return _repo.ExecuteReader<BoolResult>(ConnectionString, sql, parameters)
            .FirstOrDefault()?.Value ?? false;
    }

    private class BoolResult
    {
        public bool Value { get; set; }
    }
}
