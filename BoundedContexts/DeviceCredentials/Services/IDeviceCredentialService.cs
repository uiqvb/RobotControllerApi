using RobotControllerApi.BoundedContexts.DeviceCredentials.Dtos;

namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Services;

public interface IDeviceCredentialService
{
    List<DeviceCredentialResponse> GetDeviceCredentials();
    DeviceCredentialResponse? GetDeviceCredentialById(int id);
    List<DeviceCredentialResponse> GetDeviceCredentialsByDeviceId(int deviceId);
    CreateDeviceCredentialResponse CreateDeviceCredential(int deviceId, CreateDeviceCredentialRequest request);
    bool DeactivateDeviceCredential(int id);
    bool ReactivateDeviceCredential(int id);
    bool RevokeDeviceCredential(int id, RevokeDeviceCredentialRequest request);
    bool DeleteDeviceCredential(int id);
}
