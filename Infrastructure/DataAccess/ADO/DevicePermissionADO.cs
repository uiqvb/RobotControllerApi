using Npgsql;
using RobotControllerApi.BoundedContexts.DevicePermissions.Models;
using RobotControllerApi.BoundedContexts.DevicePermissions.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class DevicePermissionADO : IDevicePermissionDataAccess
{
    private readonly DbConfig _dbConfig;

    public DevicePermissionADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public List<DevicePermission> GetDevicePermissions()
    {
        var devicePermissions = new List<DevicePermission>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              ORDER BY id;", conn);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            devicePermissions.Add(MapDevicePermission(dr));
        }

        return devicePermissions;
    }

    public DevicePermission? GetDevicePermissionById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapDevicePermission(dr);
        }

        return null;
    }

    public List<DevicePermission> GetDevicePermissionsByUserId(int appUserId)
    {
        var devicePermissions = new List<DevicePermission>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              WHERE appuserid = @appUserId
              ORDER BY id;", conn);

        cmd.Parameters.AddWithValue("appUserId", appUserId);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            devicePermissions.Add(MapDevicePermission(dr));
        }

        return devicePermissions;
    }

    public List<DevicePermission> GetDevicePermissionsByDeviceId(int deviceId)
    {
        var devicePermissions = new List<DevicePermission>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              WHERE deviceid = @deviceId
              ORDER BY id;", conn);

        cmd.Parameters.AddWithValue("deviceId", deviceId);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            devicePermissions.Add(MapDevicePermission(dr));
        }

        return devicePermissions;
    }

    public bool DevicePermissionExists(int appUserId, int deviceId, int? excludeId = null)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        var sql = @"SELECT id
                    FROM public.devicepermission
                    WHERE appuserid = @appUserId
                      AND deviceid = @deviceId";

        if (excludeId.HasValue)
        {
            sql += " AND id <> @excludeId";
        }

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("appUserId", appUserId);
        cmd.Parameters.AddWithValue("deviceId", deviceId);

        if (excludeId.HasValue)
        {
            cmd.Parameters.AddWithValue("excludeId", excludeId.Value);
        }

        using var dr = cmd.ExecuteReader();
        return dr.Read();
    }

    public DevicePermission InsertDevicePermission(DevicePermission newDevicePermission)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.devicepermission
              (appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate)
              VALUES (@appUserId, @deviceId, @permissionLevel, @isActive, @expiresAtUtc, @createdDate, @modifiedDate)
              RETURNING id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate;", conn);

        cmd.Parameters.AddWithValue("appUserId", newDevicePermission.AppUserId);
        cmd.Parameters.AddWithValue("deviceId", newDevicePermission.DeviceId);
        cmd.Parameters.AddWithValue("permissionLevel", newDevicePermission.PermissionLevel);
        cmd.Parameters.AddWithValue("isActive", newDevicePermission.IsActive);
        cmd.Parameters.AddWithValue("expiresAtUtc", newDevicePermission.ExpiresAtUtc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("createdDate", newDevicePermission.CreatedDate);
        cmd.Parameters.AddWithValue("modifiedDate", newDevicePermission.ModifiedDate);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapDevicePermission(dr);
        }

        throw new InvalidOperationException("Failed to insert device permission.");
    }

    public bool UpdateDevicePermission(int id, DevicePermission updatedDevicePermission)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE public.devicepermission
              SET appuserid = @appUserId,
                  deviceid = @deviceId,
                  permissionlevel = @permissionLevel,
                  isactive = @isActive,
                  expiresatutc = @expiresAtUtc,
                  modifieddate = @modifiedDate
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("appUserId", updatedDevicePermission.AppUserId);
        cmd.Parameters.AddWithValue("deviceId", updatedDevicePermission.DeviceId);
        cmd.Parameters.AddWithValue("permissionLevel", updatedDevicePermission.PermissionLevel);
        cmd.Parameters.AddWithValue("isActive", updatedDevicePermission.IsActive);
        cmd.Parameters.AddWithValue("expiresAtUtc", updatedDevicePermission.ExpiresAtUtc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("modifiedDate", updatedDevicePermission.ModifiedDate);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteDevicePermission(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"DELETE FROM public.devicepermission
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    private static DevicePermission MapDevicePermission(NpgsqlDataReader dr)
    {
        return new DevicePermission
        {
            Id = dr.GetInt32(0),
            AppUserId = dr.GetInt32(1),
            DeviceId = dr.GetInt32(2),
            PermissionLevel = dr.GetString(3),
            IsActive = dr.GetBoolean(4),
            ExpiresAtUtc = dr.IsDBNull(5) ? null : dr.GetDateTime(5),
            CreatedDate = dr.GetDateTime(6),
            ModifiedDate = dr.GetDateTime(7)
        };
    }
}
