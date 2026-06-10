using RobotControllerApi.BoundedContexts.Devices.Dtos;

namespace RobotControllerApi.BoundedContexts.Devices.Services;

public interface IDeviceService
{
    List<DeviceResponse> GetDevices();
    DeviceResponse? GetDeviceById(int id);
    DeviceResponse CreateDevice(CreateDeviceRequest request);
    bool UpdateDevice(int id, UpdateDeviceRequest request);
    bool DeleteDevice(int id);
    bool AssignMap(int id, AssignMapRequest request);
    bool UnassignMap(int id);
    bool DeactivateDevice(int id);
    bool ReactivateDevice(int id);
}
