using RobotControllerApi.BoundedContexts.DeviceCapabilities.Dtos;

namespace RobotControllerApi.BoundedContexts.DeviceCapabilities.Services;

public interface IDeviceCapabilityService
{
    List<DeviceCapabilityResponse> GetDeviceCapabilities();
    DeviceCapabilityResponse? GetDeviceCapabilityById(int id);
    List<DeviceCapabilityResponse> GetDeviceCapabilitiesByDeviceId(int deviceId);
    DeviceCapabilityResponse CreateDeviceCapability(CreateDeviceCapabilityRequest request);
    bool UpdateDeviceCapability(int id, UpdateDeviceCapabilityRequest request);
    bool DeleteDeviceCapability(int id);
    bool DeactivateDeviceCapability(int id);
    bool ReactivateDeviceCapability(int id);
}
