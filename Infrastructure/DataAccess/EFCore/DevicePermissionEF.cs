using RobotControllerApi.BoundedContexts.DevicePermissions.Models;
using RobotControllerApi.BoundedContexts.DevicePermissions.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class DevicePermissionEF : IDevicePermissionDataAccess
{
    private readonly RobotContext _context;

    public DevicePermissionEF(RobotContext context)
    {
        _context = context;
    }

    public List<DevicePermission> GetDevicePermissions()
    {
        return _context.DevicePermissions
            .OrderBy(x => x.Id)
            .ToList();
    }

    public DevicePermission? GetDevicePermissionById(int id)
    {
        return _context.DevicePermissions
            .SingleOrDefault(x => x.Id == id);
    }

    public List<DevicePermission> GetDevicePermissionsByUserId(int appUserId)
    {
        return _context.DevicePermissions
            .Where(x => x.AppUserId == appUserId)
            .OrderBy(x => x.Id)
            .ToList();
    }

    public List<DevicePermission> GetDevicePermissionsByDeviceId(int deviceId)
    {
        return _context.DevicePermissions
            .Where(x => x.DeviceId == deviceId)
            .OrderBy(x => x.Id)
            .ToList();
    }

    public bool DevicePermissionExists(int appUserId, int deviceId, int? excludeId = null)
    {
        var query = _context.DevicePermissions
            .Where(x => x.AppUserId == appUserId && x.DeviceId == deviceId);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return query.Any();
    }

    public DevicePermission InsertDevicePermission(DevicePermission newDevicePermission)
    {
        _context.DevicePermissions.Add(newDevicePermission);
        _context.SaveChanges();
        return newDevicePermission;
    }

    public bool UpdateDevicePermission(int id, DevicePermission updatedDevicePermission)
    {
        var existingDevicePermission = _context.DevicePermissions
            .SingleOrDefault(x => x.Id == id);

        if (existingDevicePermission == null)
        {
            return false;
        }

        existingDevicePermission.AppUserId = updatedDevicePermission.AppUserId;
        existingDevicePermission.DeviceId = updatedDevicePermission.DeviceId;
        existingDevicePermission.PermissionLevel = updatedDevicePermission.PermissionLevel;
        existingDevicePermission.IsActive = updatedDevicePermission.IsActive;
        existingDevicePermission.ExpiresAtUtc = updatedDevicePermission.ExpiresAtUtc;
        existingDevicePermission.ModifiedDate = updatedDevicePermission.ModifiedDate;

        _context.SaveChanges();
        return true;
    }

    public bool DeleteDevicePermission(int id)
    {
        var existingDevicePermission = _context.DevicePermissions
            .SingleOrDefault(x => x.Id == id);

        if (existingDevicePermission == null)
        {
            return false;
        }

        _context.DevicePermissions.Remove(existingDevicePermission);
        _context.SaveChanges();
        return true;
    }
}
