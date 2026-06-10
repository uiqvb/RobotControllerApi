using RobotControllerApi.BoundedContexts.DevicePermissions.Dtos;

namespace RobotControllerApi.BoundedContexts.DevicePermissions.Services;

public interface IDevicePermissionService
{
    List<DevicePermissionResponse> GetDevicePermissions();
    DevicePermissionResponse? GetDevicePermissionById(int id);
    List<DevicePermissionResponse> GetDevicePermissionsByUserId(int userId);
    List<DevicePermissionResponse> GetDevicePermissionsByDeviceId(int deviceId);
    DevicePermissionResponse CreateDevicePermission(CreateDevicePermissionRequest request);
    bool UpdateDevicePermission(int id, UpdateDevicePermissionRequest request);
    bool DeactivateDevicePermission(int id);
    bool ReactivateDevicePermission(int id);
    bool DeleteDevicePermission(int id);
}
