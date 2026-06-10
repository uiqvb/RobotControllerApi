using Npgsql;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Models;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class DeviceCapabilityADO : IDeviceCapabilityDataAccess
{
    private readonly DbConfig _dbConfig;

    public DeviceCapabilityADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public List<DeviceCapability> GetDeviceCapabilities()
    {
        var deviceCapabilities = new List<DeviceCapability>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate
              FROM public.devicecapability
              ORDER BY id;", conn);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            deviceCapabilities.Add(MapDeviceCapability(dr));
        }

        return deviceCapabilities;
    }

    public DeviceCapability? GetDeviceCapabilityById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate
              FROM public.devicecapability
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapDeviceCapability(dr);
        }

        return null;
    }

    public List<DeviceCapability> GetDeviceCapabilitiesByDeviceId(int deviceId)
    {
        var deviceCapabilities = new List<DeviceCapability>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate
              FROM public.devicecapability
              WHERE deviceid = @deviceId
              ORDER BY id;", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            deviceCapabilities.Add(MapDeviceCapability(dr));
        }

        return deviceCapabilities;
    }

    public bool DeviceCapabilityExists(int deviceId, int commandCatalogueId, int? excludeId = null)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        var sql = @"SELECT id
                    FROM public.devicecapability
                    WHERE deviceid = @deviceId
                      AND commandcatalogueid = @commandCatalogueId";

        if (excludeId.HasValue)
        {
            sql += " AND id <> @excludeId";
        }

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);
        cmd.Parameters.AddWithValue("commandCatalogueId", commandCatalogueId);

        if (excludeId.HasValue)
        {
            cmd.Parameters.AddWithValue("excludeId", excludeId.Value);
        }

        using var dr = cmd.ExecuteReader();
        return dr.Read();
    }

    public DeviceCapability InsertDeviceCapability(DeviceCapability newDeviceCapability)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.devicecapability
              (deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate)
              VALUES (@deviceId, @commandCatalogueId, @requiresMap, @description, @isActive, @createdDate, @modifiedDate)
              RETURNING id, deviceid, commandcatalogueid, requiresmap, description, isactive, createddate, modifieddate;", conn);

        cmd.Parameters.AddWithValue("deviceId", newDeviceCapability.DeviceId);
        cmd.Parameters.AddWithValue("commandCatalogueId", newDeviceCapability.CommandCatalogueId);
        cmd.Parameters.AddWithValue("requiresMap", newDeviceCapability.RequiresMap);
        cmd.Parameters.AddWithValue("description", newDeviceCapability.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("isActive", newDeviceCapability.IsActive);
        cmd.Parameters.AddWithValue("createdDate", newDeviceCapability.CreatedDate);
        cmd.Parameters.AddWithValue("modifiedDate", newDeviceCapability.ModifiedDate);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapDeviceCapability(dr);
        }

        throw new InvalidOperationException("Device capability insert failed.");
    }

    public bool UpdateDeviceCapability(int id, DeviceCapability updatedDeviceCapability)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE public.devicecapability
              SET deviceid = @deviceId,
                  commandcatalogueid = @commandCatalogueId,
                  requiresmap = @requiresMap,
                  description = @description,
                  isactive = @isActive,
                  modifieddate = @modifiedDate
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("deviceId", updatedDeviceCapability.DeviceId);
        cmd.Parameters.AddWithValue("commandCatalogueId", updatedDeviceCapability.CommandCatalogueId);
        cmd.Parameters.AddWithValue("requiresMap", updatedDeviceCapability.RequiresMap);
        cmd.Parameters.AddWithValue("description", updatedDeviceCapability.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("isActive", updatedDeviceCapability.IsActive);
        cmd.Parameters.AddWithValue("modifiedDate", updatedDeviceCapability.ModifiedDate);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteDeviceCapability(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"DELETE FROM public.devicecapability
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    private static DeviceCapability MapDeviceCapability(NpgsqlDataReader dr)
    {
        return new DeviceCapability
        {
            Id = dr.GetInt32(0),
            DeviceId = dr.GetInt32(1),
            CommandCatalogueId = dr.GetInt32(2),
            RequiresMap = dr.GetBoolean(3),
            Description = dr.IsDBNull(4) ? null : dr.GetString(4),
            IsActive = dr.GetBoolean(5),
            CreatedDate = dr.GetDateTime(6),
            ModifiedDate = dr.GetDateTime(7)
        };
    }
}
