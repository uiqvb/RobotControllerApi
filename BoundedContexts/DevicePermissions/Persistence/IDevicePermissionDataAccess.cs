using RobotControllerApi.BoundedContexts.DevicePermissions.Models;

namespace RobotControllerApi.BoundedContexts.DevicePermissions.Persistence;

public interface IDevicePermissionDataAccess
{
    List<DevicePermission> GetDevicePermissions();
    DevicePermission? GetDevicePermissionById(int id);
    List<DevicePermission> GetDevicePermissionsByUserId(int appUserId);
    List<DevicePermission> GetDevicePermissionsByDeviceId(int deviceId);
    bool DevicePermissionExists(int appUserId, int deviceId, int? excludeId = null);
    DevicePermission InsertDevicePermission(DevicePermission newDevicePermission);
    bool UpdateDevicePermission(int id, DevicePermission updatedDevicePermission);
    bool DeleteDevicePermission(int id);
}
