using RobotControllerApi.BoundedContexts.Devices.Persistence;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Dtos;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Models;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Persistence;
using RobotControllerApi.BoundedContexts.Maps.Persistence;

namespace RobotControllerApi.BoundedContexts.DeviceStatuses.Services;

public class DeviceStatusService : IDeviceStatusService
{
    private readonly IDeviceStatusDataAccess _dataAccess;
    private readonly IDeviceDataAccess _deviceDataAccess;
    private readonly IMapDataAccess _mapDataAccess;

    private static readonly string[] ConnectionStates = { "Unknown", "Online", "Offline" };
    private static readonly string[] OperationalStates = { "Unknown", "Idle", "Executing", "Paused", "Stopped", "Faulted" };
    private static readonly string[] FacingValues = { "North", "East", "South", "West" };

    public DeviceStatusService(IDeviceStatusDataAccess dataAccess, IDeviceDataAccess deviceDataAccess, IMapDataAccess mapDataAccess)
    {
        _dataAccess = dataAccess;
        _deviceDataAccess = deviceDataAccess;
        _mapDataAccess = mapDataAccess;
    }

    public List<DeviceStatusResponse> GetDeviceStatuses()
    {
        return _dataAccess.GetDeviceStatuses().Select(MapToResponse).ToList();
    }

    public DeviceStatusResponse? GetDeviceStatusById(int id)
    {
        var model = _dataAccess.GetDeviceStatusById(id);
        return model == null ? null : MapToResponse(model);
    }

    public DeviceStatusResponse? GetDeviceStatusByDeviceId(int deviceId)
    {
        var model = _dataAccess.GetDeviceStatusByDeviceId(deviceId);
        return model == null ? null : MapToResponse(model);
    }

    public bool UpdateDeviceStatus(int deviceId, UpdateDeviceStatusRequest request)
    {
        var status = TryGetStatusForDevice(deviceId);
        if (status == null) return false;
        ValidateState(request.ConnectionState, request.OperationalState);
        status.ConnectionState = request.ConnectionState;
        status.OperationalState = request.OperationalState;
        status.StatusMessage = request.StatusMessage;
        status.LastErrorCode = request.LastErrorCode;
        status.LastErrorMessage = request.LastErrorMessage;
        status.LastSeenAtUtc = DateTime.UtcNow;
        status.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateDeviceStatus(status.Id, status);
    }

    public bool RecordHeartbeat(int deviceId, HeartbeatRequest request)
    {
        var status = TryGetStatusForDevice(deviceId);
        if (status == null) return false;
        status.ConnectionState = "Online";
        status.LastSeenAtUtc = DateTime.UtcNow;
        status.LastHeartbeatAtUtc = DateTime.UtcNow;
        status.StatusMessage = request.StatusMessage;
        status.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateDeviceStatus(status.Id, status);
    }

    public bool UpdateGridPose(int deviceId, UpdateGridPoseRequest request)
    {
        var status = TryGetStatusForDevice(deviceId);
        if (status == null) return false;
        status.PoseMapId = request.PoseMapId;
        status.GridX = request.GridX;
        status.GridY = request.GridY;
        status.Facing = request.Facing;
        status.IsGridAligned = request.IsGridAligned;
        status.IsGridPoseTrusted = request.IsGridPoseTrusted;
        status.PoseConfidence = request.PoseConfidence;
        status.EstimatedXcm = request.EstimatedXcm;
        status.EstimatedYcm = request.EstimatedYcm;
        status.EstimatedHeadingDegrees = request.EstimatedHeadingDegrees;
        status.IsInsideMap = request.IsInsideMap;
        status.StatusMessage = request.StatusMessage;
        ValidateGridPose(status);
        status.LastSeenAtUtc = DateTime.UtcNow;
        status.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateDeviceStatus(status.Id, status);
    }

    public bool InvalidateGridPose(int deviceId, InvalidateGridPoseRequest request)
    {
        var status = TryGetStatusForDevice(deviceId);
        if (status == null) return false;
        status.IsGridPoseTrusted = false;
        status.IsGridAligned = false;
        status.PoseConfidence = null;
        status.StatusMessage = string.IsNullOrWhiteSpace(request.Reason) ? "Grid pose invalidated." : request.Reason;
        status.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateDeviceStatus(status.Id, status);
    }

    private DeviceStatus? TryGetStatusForDevice(int deviceId)
    {
        var device = _deviceDataAccess.GetDeviceById(deviceId);
        if (device == null) return null;
        return _dataAccess.GetDeviceStatusByDeviceId(deviceId);
    }

    private void ValidateState(string connectionState, string operationalState)
    {
        if (!ConnectionStates.Contains(connectionState)) throw new ArgumentException("ConnectionState must be Unknown, Online, or Offline.");
        if (!OperationalStates.Contains(operationalState)) throw new ArgumentException("OperationalState must be Unknown, Idle, Executing, Paused, Stopped, or Faulted.");
    }

    private void ValidateGridPose(DeviceStatus status)
    {
        var gridFields = new[] { status.GridX.HasValue, status.GridY.HasValue, !string.IsNullOrWhiteSpace(status.Facing) };
        if (gridFields.Any(x => x) && gridFields.Any(x => !x)) throw new ArgumentException("GridX, GridY, and Facing must be supplied together or all omitted.");
        if (status.Facing != null && !FacingValues.Contains(status.Facing)) throw new ArgumentException("Facing must be null, North, East, South, or West.");
        if (status.PoseConfidence.HasValue && (status.PoseConfidence.Value < 0 || status.PoseConfidence.Value > 1)) throw new ArgumentException("PoseConfidence must be between 0 and 1.");
        if (status.EstimatedHeadingDegrees.HasValue && (status.EstimatedHeadingDegrees.Value < 0 || status.EstimatedHeadingDegrees.Value >= 360)) throw new ArgumentException("EstimatedHeadingDegrees must be greater than or equal to 0 and less than 360.");
        if (!status.IsGridPoseTrusted) return;
        if (!status.PoseMapId.HasValue) throw new ArgumentException("PoseMapId is required when the grid pose is trusted.");
        var device = _deviceDataAccess.GetDeviceById(status.DeviceId);
        if (device == null) throw new ArgumentException("Device does not exist.");
        if (!device.MapId.HasValue) throw new ArgumentException("Device MapId is required when the grid pose is trusted.");
        if (status.PoseMapId.Value != device.MapId.Value) throw new ArgumentException("PoseMapId must match the device MapId.");
        var map = _mapDataAccess.GetMapById(status.PoseMapId.Value);
        if (map == null || !map.IsActive) throw new ArgumentException("Pose map must exist and be active.");
        if (!status.GridX.HasValue || !status.GridY.HasValue) throw new ArgumentException("GridX and GridY are required when the grid pose is trusted.");
        if (status.GridX.Value < 0 || status.GridX.Value >= map.Columns || status.GridY.Value < 0 || status.GridY.Value >= map.Rows) throw new ArgumentException("GridX and GridY must be inside the map.");
        if (!status.IsGridAligned) throw new ArgumentException("IsGridAligned must be true when the grid pose is trusted.");
    }

    private static DeviceStatusResponse MapToResponse(DeviceStatus model)
    {
        return new DeviceStatusResponse
        {
            Id = model.Id,
            DeviceId = model.DeviceId,
            ConnectionState = model.ConnectionState,
            OperationalState = model.OperationalState,
            LastSeenAtUtc = model.LastSeenAtUtc,
            LastHeartbeatAtUtc = model.LastHeartbeatAtUtc,
            PoseMapId = model.PoseMapId,
            GridX = model.GridX,
            GridY = model.GridY,
            Facing = model.Facing,
            IsGridAligned = model.IsGridAligned,
            IsGridPoseTrusted = model.IsGridPoseTrusted,
            PoseConfidence = model.PoseConfidence,
            EstimatedXcm = model.EstimatedXcm,
            EstimatedYcm = model.EstimatedYcm,
            EstimatedHeadingDegrees = model.EstimatedHeadingDegrees,
            IsInsideMap = model.IsInsideMap,
            StatusMessage = model.StatusMessage,
            LastErrorCode = model.LastErrorCode,
            LastErrorMessage = model.LastErrorMessage,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };
    }
}
