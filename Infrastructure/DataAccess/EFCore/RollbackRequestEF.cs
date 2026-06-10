using Microsoft.EntityFrameworkCore;
using Npgsql;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class RollbackRequestEF : IRollbackRequestDataAccess
{
    private readonly RobotContext _context;

    public RollbackRequestEF(RobotContext context)
    {
        _context = context;
    }

    public List<RollbackRequest> GetRollbackRequests()
    {
        return _context.RollbackRequests
            .OrderBy(x => x.Id)
            .ToList();
    }

    public RollbackRequest? GetRollbackRequestById(int id)
    {
        return _context.RollbackRequests
            .SingleOrDefault(x => x.Id == id);
    }

    public List<RollbackRequest> GetRollbackRequestsByDeviceId(int deviceId)
    {
        return _context.RollbackRequests
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    public RollbackRequest InsertRollbackRequest(RollbackRequest newRollbackRequest)
    {
        _context.RollbackRequests.Add(newRollbackRequest);
        _context.SaveChanges();
        return newRollbackRequest;
    }

    public bool UpdateRollbackRequest(int id, RollbackRequest updatedRollbackRequest)
    {
        var existing = _context.RollbackRequests
            .SingleOrDefault(x => x.Id == id);

        if (existing == null) return false;

        existing.DeviceId = updatedRollbackRequest.DeviceId;
        existing.RequestedByAppUserId = updatedRollbackRequest.RequestedByAppUserId;
        existing.RequestedByProvider = updatedRollbackRequest.RequestedByProvider;
        existing.TargetType = updatedRollbackRequest.TargetType;
        existing.TargetIdsJson = updatedRollbackRequest.TargetIdsJson;
        existing.Status = updatedRollbackRequest.Status;
        existing.GeneratedWorkflowId = updatedRollbackRequest.GeneratedWorkflowId;
        existing.GeneratedJobId = updatedRollbackRequest.GeneratedJobId;
        existing.FailureCode = updatedRollbackRequest.FailureCode;
        existing.FailureMessage = updatedRollbackRequest.FailureMessage;
        existing.ModifiedDate = updatedRollbackRequest.ModifiedDate;

        return _context.SaveChanges() > 0;
    }

    public bool DeviceExistsAndActive(int deviceId)
    {
        return _context.Devices
            .Any(x => x.Id == deviceId && x.IsActive);
    }

    public bool HasGeneratedRollbackForTarget(string targetType, string targetIdsJson)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM public.rollbackrequest WHERE targettype = @targetType AND targetidsjson = @targetIdsJson::jsonb AND status = 'Generated')";
        return ReadBool(sql, new[]
        {
            new NpgsqlParameter("targetType", targetType),
            new NpgsqlParameter("targetIdsJson", targetIdsJson)
        });
    }

    public CommandCatalogue? GetActiveCommandCatalogueById(int id)
    {
        return _context.CommandCatalogues
            .SingleOrDefault(x => x.Id == id && x.IsActive);
    }

    public CommandCatalogue? GetActiveCommandCatalogueByName(string name)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return _context.CommandCatalogues
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.IsActive && x.Name.ToUpper() == normalized);
    }

    private bool ReadBool(string sql, NpgsqlParameter[] parameters)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);

        if (command.Connection == null) return false;

        if (command.Connection.State != System.Data.ConnectionState.Open)
        {
            command.Connection.Open();
        }

        var result = command.ExecuteScalar();
        return result is bool value && value;
    }
}
