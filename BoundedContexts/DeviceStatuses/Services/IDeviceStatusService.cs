using RobotControllerApi.BoundedContexts.DeviceStatuses.Dtos;

namespace RobotControllerApi.BoundedContexts.DeviceStatuses.Services;

public interface IDeviceStatusService
{
    List<DeviceStatusResponse> GetDeviceStatuses();
    DeviceStatusResponse? GetDeviceStatusById(int id);
    DeviceStatusResponse? GetDeviceStatusByDeviceId(int deviceId);
    bool UpdateDeviceStatus(int deviceId, UpdateDeviceStatusRequest request);
    bool RecordHeartbeat(int deviceId, HeartbeatRequest request);
    bool UpdateGridPose(int deviceId, UpdateGridPoseRequest request);
    bool InvalidateGridPose(int deviceId, InvalidateGridPoseRequest request);
}
