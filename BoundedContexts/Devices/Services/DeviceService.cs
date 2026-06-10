using RobotControllerApi.BoundedContexts.Devices.Dtos;
using RobotControllerApi.BoundedContexts.Devices.Models;
using RobotControllerApi.BoundedContexts.Devices.Persistence;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Models;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Persistence;
using RobotControllerApi.BoundedContexts.Maps.Persistence;

namespace RobotControllerApi.BoundedContexts.Devices.Services;

public class DeviceService : IDeviceService
{
    private readonly IDeviceDataAccess _dataAccess;
    private readonly IMapDataAccess _mapDataAccess;
    private readonly IDeviceStatusDataAccess _statusDataAccess;

    public DeviceService(IDeviceDataAccess dataAccess, IMapDataAccess mapDataAccess, IDeviceStatusDataAccess statusDataAccess)
    {
        _dataAccess = dataAccess;
        _mapDataAccess = mapDataAccess;
        _statusDataAccess = statusDataAccess;
    }

    public List<DeviceResponse> GetDevices()
    {
        return _dataAccess.GetDevices().Select(MapToResponse).ToList();
    }

    public DeviceResponse? GetDeviceById(int id)
    {
        var model = _dataAccess.GetDeviceById(id);
        return model == null ? null : MapToResponse(model);
    }

    public DeviceResponse CreateDevice(CreateDeviceRequest request)
    {
        ValidateRequest(request.Name, request.DeviceIdentifier, request.DeviceType, request.MapId);
        EnsureIdentifierUnique(request.DeviceIdentifier, null);

        var now = DateTime.UtcNow;
        var model = new Device
        {
            Name = request.Name.Trim(),
            DeviceIdentifier = request.DeviceIdentifier.Trim(),
            DeviceType = request.DeviceType.Trim(),
            MapId = request.MapId,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedDate = now,
            ModifiedDate = now
        };

        var created = _dataAccess.InsertDevice(model);

        _statusDataAccess.InsertDeviceStatus(new DeviceStatus
        {
            DeviceId = created.Id,
            ConnectionState = "Unknown",
            OperationalState = "Unknown",
            CreatedDate = now,
            ModifiedDate = now
        });

        return MapToResponse(created);
    }

    public bool UpdateDevice(int id, UpdateDeviceRequest request)
    {
        ValidateRequest(request.Name, request.DeviceIdentifier, request.DeviceType, request.MapId);

        var existing = _dataAccess.GetDeviceById(id);
        if (existing == null) return false;

        EnsureIdentifierUnique(request.DeviceIdentifier, id);

        var mapChanged = existing.MapId != request.MapId;
        var updatedDevice = new Device
        {
            Id = id,
            Name = request.Name.Trim(),
            DeviceIdentifier = request.DeviceIdentifier.Trim(),
            DeviceType = request.DeviceType.Trim(),
            MapId = request.MapId,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedDate = existing.CreatedDate,
            ModifiedDate = DateTime.UtcNow
        };

        var updated = _dataAccess.UpdateDevice(id, updatedDevice);
        if (updated && mapChanged) InvalidateTrustedGridPose(id, "Device map assignment changed.");
        return updated;
    }

    public bool DeleteDevice(int id)
    {
        if (_dataAccess.GetDeviceById(id) == null) return false;
        return _dataAccess.DeleteDevice(id);
    }

    public bool AssignMap(int id, AssignMapRequest request)
    {
        var existing = _dataAccess.GetDeviceById(id);
        if (existing == null) return false;

        ValidateMap(request.MapId);

        existing.MapId = request.MapId;
        existing.ModifiedDate = DateTime.UtcNow;

        var updated = _dataAccess.UpdateDevice(id, existing);
        if (updated) InvalidateTrustedGridPose(id, "Device assigned to a map.");
        return updated;
    }

    public bool UnassignMap(int id)
    {
        var existing = _dataAccess.GetDeviceById(id);
        if (existing == null) return false;

        existing.MapId = null;
        existing.ModifiedDate = DateTime.UtcNow;

        var updated = _dataAccess.UpdateDevice(id, existing);
        if (updated) InvalidateTrustedGridPose(id, "Device unassigned from its map.");
        return updated;
    }

    public bool DeactivateDevice(int id)
    {
        var existing = _dataAccess.GetDeviceById(id);
        if (existing == null) return false;

        existing.IsActive = false;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateDevice(id, existing);
    }

    public bool ReactivateDevice(int id)
    {
        var existing = _dataAccess.GetDeviceById(id);
        if (existing == null) return false;

        existing.IsActive = true;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateDevice(id, existing);
    }

    private void ValidateRequest(string name, string deviceIdentifier, string deviceType, int? mapId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(deviceIdentifier)) throw new ArgumentException("DeviceIdentifier is required.");
        if (string.IsNullOrWhiteSpace(deviceType)) throw new ArgumentException("DeviceType is required.");
        if (mapId.HasValue) ValidateMap(mapId.Value);
    }

    private void ValidateMap(int mapId)
    {
        var map = _mapDataAccess.GetMapById(mapId);
        if (map == null || !map.IsActive) throw new ArgumentException("Map must exist and be active.");
    }

    private void EnsureIdentifierUnique(string identifier, int? currentId)
    {
        if (_dataAccess.DeviceExistsByIdentifier(identifier.Trim(), currentId))
        {
            throw new InvalidOperationException("A device with this identifier already exists.");
        }
    }

    private void InvalidateTrustedGridPose(int deviceId, string message)
    {
        var status = _statusDataAccess.GetDeviceStatusByDeviceId(deviceId);
        if (status == null) return;

        status.IsGridPoseTrusted = false;
        status.IsGridAligned = false;
        status.PoseConfidence = null;
        status.StatusMessage = message;
        status.ModifiedDate = DateTime.UtcNow;

        _statusDataAccess.UpdateDeviceStatus(status.Id, status);
    }

    private static DeviceResponse MapToResponse(Device model)
    {
        return new DeviceResponse
        {
            Id = model.Id,
            Name = model.Name,
            DeviceIdentifier = model.DeviceIdentifier,
            DeviceType = model.DeviceType,
            MapId = model.MapId,
            Description = model.Description,
            IsActive = model.IsActive,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };
    }
}
