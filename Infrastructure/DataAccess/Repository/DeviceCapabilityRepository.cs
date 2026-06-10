using Npgsql;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Models;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class DeviceCapabilityRepository : IDeviceCapabilityDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public DeviceCapabilityRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public List<DeviceCapability> GetDeviceCapabilities()
        => _repo.ExecuteReader<DeviceCapability>(
            ConnectionString,
            @"SELECT id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate
              FROM public.devicecapability
              ORDER BY id;");

    public DeviceCapability? GetDeviceCapabilityById(int id)
        => _repo.ExecuteReader<DeviceCapability>(
            ConnectionString,
            @"SELECT id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate
              FROM public.devicecapability
              WHERE id = @id;",
            new[] { new NpgsqlParameter("id", id) }
        ).SingleOrDefault();

    public List<DeviceCapability> GetDeviceCapabilitiesByDeviceId(int deviceId)
        => _repo.ExecuteReader<DeviceCapability>(
            ConnectionString,
            @"SELECT id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate
              FROM public.devicecapability
              WHERE deviceid = @deviceId
              ORDER BY id;",
            new[] { new NpgsqlParameter("deviceId", deviceId) }
        );

    public bool DeviceCapabilityExists(int deviceId, int commandCatalogueId, int? excludeId = null)
        => GetDeviceCapabilities().Any(x =>
            x.DeviceId == deviceId &&
            x.CommandCatalogueId == commandCatalogueId &&
            (!excludeId.HasValue || x.Id != excludeId.Value));

    public DeviceCapability InsertDeviceCapability(DeviceCapability newDeviceCapability)
    {
        var result = _repo.ExecuteReader<DeviceCapability>(
            ConnectionString,
            @"INSERT INTO public.devicecapability
              (deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate)
              VALUES (@deviceId, @commandCatalogueId, @requiresMap, @description, @isActive, @createdDate, @modifiedDate)
              RETURNING id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate;",
            new[]
            {
                new NpgsqlParameter("deviceId", newDeviceCapability.DeviceId),
                new NpgsqlParameter("commandCatalogueId", newDeviceCapability.CommandCatalogueId),
                new NpgsqlParameter("requiresMap", newDeviceCapability.RequiresMap),
                new NpgsqlParameter("description", newDeviceCapability.Description ?? (object)DBNull.Value),
                new NpgsqlParameter("isActive", newDeviceCapability.IsActive),
                new NpgsqlParameter("createdDate", newDeviceCapability.CreatedDate),
                new NpgsqlParameter("modifiedDate", newDeviceCapability.ModifiedDate)
            }
        ).Single();

        return result;
    }

    public bool UpdateDeviceCapability(int id, DeviceCapability updatedDeviceCapability)
    {
        var result = _repo.ExecuteReader<DeviceCapability>(
            ConnectionString,
            @"UPDATE public.devicecapability
              SET deviceid = @deviceId,
                  commandcatalogueid = @commandCatalogueId,
                  requiresmap = @requiresMap,
                  description = @description,
                  isactive = @isActive,
                  modifieddate = @modifiedDate
              WHERE id = @id
              RETURNING id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate;",
            new[]
            {
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("deviceId", updatedDeviceCapability.DeviceId),
                new NpgsqlParameter("commandCatalogueId", updatedDeviceCapability.CommandCatalogueId),
                new NpgsqlParameter("requiresMap", updatedDeviceCapability.RequiresMap),
                new NpgsqlParameter("description", updatedDeviceCapability.Description ?? (object)DBNull.Value),
                new NpgsqlParameter("isActive", updatedDeviceCapability.IsActive),
                new NpgsqlParameter("modifiedDate", updatedDeviceCapability.ModifiedDate)
            }
        );

        return result.Any();
    }

    public bool DeleteDeviceCapability(int id)
    {
        var result = _repo.ExecuteReader<DeviceCapability>(
            ConnectionString,
            @"DELETE FROM public.devicecapability
              WHERE id = @id
              RETURNING id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate;",
            new[] { new NpgsqlParameter("id", id) }
        );

        return result.Any();
    }
}
