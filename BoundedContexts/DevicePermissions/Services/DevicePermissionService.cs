using RobotControllerApi.BoundedContexts.AppUsers.Persistence;
using RobotControllerApi.BoundedContexts.Devices.Persistence;
using RobotControllerApi.BoundedContexts.DevicePermissions.Dtos;
using RobotControllerApi.BoundedContexts.DevicePermissions.Models;
using RobotControllerApi.BoundedContexts.DevicePermissions.Persistence;

namespace RobotControllerApi.BoundedContexts.DevicePermissions.Services;

public class DevicePermissionService : IDevicePermissionService
{
    private readonly IDevicePermissionDataAccess _dataAccess;
    private readonly IAppUserDataAccess _appUserDataAccess;
    private readonly IDeviceDataAccess _deviceDataAccess;
    private static readonly string[] PermissionLevels = { "Viewer", "Operator", "Manager", "Owner" };

    public DevicePermissionService(IDevicePermissionDataAccess dataAccess, IAppUserDataAccess appUserDataAccess, IDeviceDataAccess deviceDataAccess)
    {
        _dataAccess = dataAccess;
        _appUserDataAccess = appUserDataAccess;
        _deviceDataAccess = deviceDataAccess;
    }

    public List<DevicePermissionResponse> GetDevicePermissions()
    {
        return _dataAccess.GetDevicePermissions().Select(MapToResponse).ToList();
    }

    public DevicePermissionResponse? GetDevicePermissionById(int id)
    {
        var model = _dataAccess.GetDevicePermissionById(id);
        return model == null ? null : MapToResponse(model);
    }

    public List<DevicePermissionResponse> GetDevicePermissionsByUserId(int userId)
    {
        return _dataAccess.GetDevicePermissionsByUserId(userId).Select(MapToResponse).ToList();
    }

    public List<DevicePermissionResponse> GetDevicePermissionsByDeviceId(int deviceId)
    {
        return _dataAccess.GetDevicePermissionsByDeviceId(deviceId).Select(MapToResponse).ToList();
    }

    public DevicePermissionResponse CreateDevicePermission(CreateDevicePermissionRequest request)
    {
        ValidateRequest(request.AppUserId, request.DeviceId, request.PermissionLevel, request.ExpiresAtUtc);
        EnsurePairUnique(request.AppUserId, request.DeviceId, null);

        var now = DateTime.UtcNow;

        var model = new DevicePermission
        {
            AppUserId = request.AppUserId,
            DeviceId = request.DeviceId,
            PermissionLevel = request.PermissionLevel,
            IsActive = request.IsActive,
            ExpiresAtUtc = request.ExpiresAtUtc,
            CreatedDate = now,
            ModifiedDate = now
        };

        return MapToResponse(_dataAccess.InsertDevicePermission(model));
    }

    public bool UpdateDevicePermission(int id, UpdateDevicePermissionRequest request)
    {
        var existing = _dataAccess.GetDevicePermissionById(id);
        if (existing == null)
        {
            return false;
        }

        ValidateRequest(request.AppUserId, request.DeviceId, request.PermissionLevel, request.ExpiresAtUtc);
        EnsurePairUnique(request.AppUserId, request.DeviceId, id);

        var updated = new DevicePermission
        {
            Id = id,
            AppUserId = request.AppUserId,
            DeviceId = request.DeviceId,
            PermissionLevel = request.PermissionLevel,
            IsActive = request.IsActive,
            ExpiresAtUtc = request.ExpiresAtUtc,
            CreatedDate = existing.CreatedDate,
            ModifiedDate = DateTime.UtcNow
        };

        return _dataAccess.UpdateDevicePermission(id, updated);
    }

    public bool DeactivateDevicePermission(int id)
    {
        var existing = _dataAccess.GetDevicePermissionById(id);
        if (existing == null)
        {
            return false;
        }

        existing.IsActive = false;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateDevicePermission(id, existing);
    }

    public bool ReactivateDevicePermission(int id)
    {
        var existing = _dataAccess.GetDevicePermissionById(id);
        if (existing == null)
        {
            return false;
        }

        ValidateRequest(existing.AppUserId, existing.DeviceId, existing.PermissionLevel, existing.ExpiresAtUtc);

        existing.IsActive = true;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateDevicePermission(id, existing);
    }

    public bool DeleteDevicePermission(int id)
    {
        if (_dataAccess.GetDevicePermissionById(id) == null)
        {
            return false;
        }

        return _dataAccess.DeleteDevicePermission(id);
    }

    private void ValidateRequest(int userId, int deviceId, string permissionLevel, DateTime? expiresAtUtc)
    {
        var user = _appUserDataAccess.GetAppUserById(userId);
        if (user == null || !user.IsActive)
        {
            throw new ArgumentException("AppUser must exist and be active.");
        }

        var device = _deviceDataAccess.GetDeviceById(deviceId);
        if (device == null || !device.IsActive)
        {
            throw new ArgumentException("Device must exist and be active.");
        }

        if (!PermissionLevels.Contains(permissionLevel))
        {
            throw new ArgumentException("PermissionLevel must be Viewer, Operator, Manager, or Owner.");
        }

        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new ArgumentException("ExpiresAtUtc must be in the future.");
        }
    }

    private void EnsurePairUnique(int userId, int deviceId, int? currentId)
    {
        if (_dataAccess.DevicePermissionExists(userId, deviceId, currentId))
        {
            throw new InvalidOperationException("This device permission already exists.");
        }
    }

    private static DevicePermissionResponse MapToResponse(DevicePermission model)
    {
        return new DevicePermissionResponse
        {
            Id = model.Id,
            AppUserId = model.AppUserId,
            DeviceId = model.DeviceId,
            PermissionLevel = model.PermissionLevel,
            IsActive = model.IsActive,
            ExpiresAtUtc = model.ExpiresAtUtc,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };
    }
}
