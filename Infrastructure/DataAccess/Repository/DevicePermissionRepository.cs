using Npgsql;
using RobotControllerApi.BoundedContexts.DevicePermissions.Models;
using RobotControllerApi.BoundedContexts.DevicePermissions.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class DevicePermissionRepository : IDevicePermissionDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public DevicePermissionRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public List<DevicePermission> GetDevicePermissions()
        => _repo.ExecuteReader<DevicePermission>(
            ConnectionString,
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              ORDER BY id;");

    public DevicePermission? GetDevicePermissionById(int id)
        => _repo.ExecuteReader<DevicePermission>(
            ConnectionString,
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              WHERE id = @id;",
            new[] { new NpgsqlParameter("id", id) }
        ).SingleOrDefault();

    public List<DevicePermission> GetDevicePermissionsByUserId(int appUserId)
        => _repo.ExecuteReader<DevicePermission>(
            ConnectionString,
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              WHERE appuserid = @appUserId
              ORDER BY id;",
            new[] { new NpgsqlParameter("appUserId", appUserId) }
        );

    public List<DevicePermission> GetDevicePermissionsByDeviceId(int deviceId)
        => _repo.ExecuteReader<DevicePermission>(
            ConnectionString,
            @"SELECT id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate
              FROM public.devicepermission
              WHERE deviceid = @deviceId
              ORDER BY id;",
            new[] { new NpgsqlParameter("deviceId", deviceId) }
        );

    public bool DevicePermissionExists(int appUserId, int deviceId, int? excludeId = null)
        => GetDevicePermissions().Any(x =>
            x.AppUserId == appUserId &&
            x.DeviceId == deviceId &&
            (!excludeId.HasValue || x.Id != excludeId.Value));

    public DevicePermission InsertDevicePermission(DevicePermission newDevicePermission)
    {
        var result = _repo.ExecuteReader<DevicePermission>(
            ConnectionString,
            @"INSERT INTO public.devicepermission
              (appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate)
              VALUES (@appUserId, @deviceId, @permissionLevel, @isActive, @expiresAtUtc, @createdDate, @modifiedDate)
              RETURNING id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate;",
            new[]
            {
                new NpgsqlParameter("appUserId", newDevicePermission.AppUserId),
                new NpgsqlParameter("deviceId", newDevicePermission.DeviceId),
                new NpgsqlParameter("permissionLevel", newDevicePermission.PermissionLevel),
                new NpgsqlParameter("isActive", newDevicePermission.IsActive),
                new NpgsqlParameter("expiresAtUtc", newDevicePermission.ExpiresAtUtc ?? (object)DBNull.Value),
                new NpgsqlParameter("createdDate", newDevicePermission.CreatedDate),
                new NpgsqlParameter("modifiedDate", newDevicePermission.ModifiedDate)
            }
        ).Single();

        return result;
    }

    public bool UpdateDevicePermission(int id, DevicePermission updatedDevicePermission)
    {
        var result = _repo.ExecuteReader<DevicePermission>(
            ConnectionString,
            @"UPDATE public.devicepermission
              SET appuserid = @appUserId,
                  deviceid = @deviceId,
                  permissionlevel = @permissionLevel,
                  isactive = @isActive,
                  expiresatutc = @expiresAtUtc,
                  modifieddate = @modifiedDate
              WHERE id = @id
              RETURNING id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate;",
            new[]
            {
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("appUserId", updatedDevicePermission.AppUserId),
                new NpgsqlParameter("deviceId", updatedDevicePermission.DeviceId),
                new NpgsqlParameter("permissionLevel", updatedDevicePermission.PermissionLevel),
                new NpgsqlParameter("isActive", updatedDevicePermission.IsActive),
                new NpgsqlParameter("expiresAtUtc", updatedDevicePermission.ExpiresAtUtc ?? (object)DBNull.Value),
                new NpgsqlParameter("modifiedDate", updatedDevicePermission.ModifiedDate)
            }
        );

        return result.Any();
    }

    public bool DeleteDevicePermission(int id)
    {
        var result = _repo.ExecuteReader<DevicePermission>(
            ConnectionString,
            @"DELETE FROM public.devicepermission
              WHERE id = @id
              RETURNING id, appuserid, deviceid, permissionlevel, isactive, expiresatutc, createddate, modifieddate;",
            new[] { new NpgsqlParameter("id", id) }
        );

        return result.Any();
    }
}
