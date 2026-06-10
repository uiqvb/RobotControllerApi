using RobotControllerApi.BoundedContexts.DeviceStatuses.Models;

namespace RobotControllerApi.BoundedContexts.DeviceStatuses.Persistence;

public interface IDeviceStatusDataAccess
{
    List<DeviceStatus> GetDeviceStatuses();
    DeviceStatus? GetDeviceStatusById(int id);
    DeviceStatus? GetDeviceStatusByDeviceId(int deviceId);
    bool DeviceStatusExistsByDeviceId(int deviceId, int? excludeId = null);
    DeviceStatus InsertDeviceStatus(DeviceStatus newDeviceStatus);
    bool UpdateDeviceStatus(int id, DeviceStatus updatedDeviceStatus);
}
