using RobotControllerApi.BoundedContexts.DeviceCapabilities.Models;

namespace RobotControllerApi.BoundedContexts.DeviceCapabilities.Persistence;

public interface IDeviceCapabilityDataAccess
{
    List<DeviceCapability> GetDeviceCapabilities();
    DeviceCapability? GetDeviceCapabilityById(int id);
    List<DeviceCapability> GetDeviceCapabilitiesByDeviceId(int deviceId);
    bool DeviceCapabilityExists(int deviceId, int commandCatalogueId, int? excludeId = null);
    DeviceCapability InsertDeviceCapability(DeviceCapability newDeviceCapability);
    bool UpdateDeviceCapability(int id, DeviceCapability updatedDeviceCapability);
    bool DeleteDeviceCapability(int id);
}
