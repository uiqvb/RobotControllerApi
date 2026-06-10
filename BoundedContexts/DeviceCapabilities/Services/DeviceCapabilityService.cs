using RobotControllerApi.BoundedContexts.CommandCatalogues.Persistence;
using RobotControllerApi.BoundedContexts.Devices.Persistence;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Dtos;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Models;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Persistence;

namespace RobotControllerApi.BoundedContexts.DeviceCapabilities.Services;

public class DeviceCapabilityService : IDeviceCapabilityService
{
    private readonly IDeviceCapabilityDataAccess _dataAccess;
    private readonly IDeviceDataAccess _deviceDataAccess;
    private readonly ICommandCatalogueDataAccess _commandCatalogueDataAccess;

    public DeviceCapabilityService(IDeviceCapabilityDataAccess dataAccess, IDeviceDataAccess deviceDataAccess, ICommandCatalogueDataAccess commandCatalogueDataAccess)
    {
        _dataAccess = dataAccess;
        _deviceDataAccess = deviceDataAccess;
        _commandCatalogueDataAccess = commandCatalogueDataAccess;
    }

    public List<DeviceCapabilityResponse> GetDeviceCapabilities()
    {
        return _dataAccess.GetDeviceCapabilities().Select(MapToResponse).ToList();
    }

    public DeviceCapabilityResponse? GetDeviceCapabilityById(int id)
    {
        var model = _dataAccess.GetDeviceCapabilityById(id);
        return model == null ? null : MapToResponse(model);
    }

    public List<DeviceCapabilityResponse> GetDeviceCapabilitiesByDeviceId(int deviceId)
    {
        return _dataAccess.GetDeviceCapabilitiesByDeviceId(deviceId).Select(MapToResponse).ToList();
    }

    public DeviceCapabilityResponse CreateDeviceCapability(CreateDeviceCapabilityRequest request)
    {
        ValidateReferences(request.DeviceId, request.CommandCatalogueId);
        EnsurePairUnique(request.DeviceId, request.CommandCatalogueId, null);

        var now = DateTime.UtcNow;

        var model = new DeviceCapability
        {
            DeviceId = request.DeviceId,
            CommandCatalogueId = request.CommandCatalogueId,
            RequiresMap = request.RequiresMap,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedDate = now,
            ModifiedDate = now
        };

        return MapToResponse(_dataAccess.InsertDeviceCapability(model));
    }

    public bool UpdateDeviceCapability(int id, UpdateDeviceCapabilityRequest request)
    {
        var existing = _dataAccess.GetDeviceCapabilityById(id);
        if (existing == null)
        {
            return false;
        }

        ValidateReferences(request.DeviceId, request.CommandCatalogueId);
        EnsurePairUnique(request.DeviceId, request.CommandCatalogueId, id);

        var updated = new DeviceCapability
        {
            Id = id,
            DeviceId = request.DeviceId,
            CommandCatalogueId = request.CommandCatalogueId,
            RequiresMap = request.RequiresMap,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedDate = existing.CreatedDate,
            ModifiedDate = DateTime.UtcNow
        };

        return _dataAccess.UpdateDeviceCapability(id, updated);
    }

    public bool DeleteDeviceCapability(int id)
    {
        if (_dataAccess.GetDeviceCapabilityById(id) == null)
        {
            return false;
        }

        return _dataAccess.DeleteDeviceCapability(id);
    }

    public bool DeactivateDeviceCapability(int id)
    {
        var existing = _dataAccess.GetDeviceCapabilityById(id);
        if (existing == null)
        {
            return false;
        }

        existing.IsActive = false;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateDeviceCapability(id, existing);
    }

    public bool ReactivateDeviceCapability(int id)
    {
        var existing = _dataAccess.GetDeviceCapabilityById(id);
        if (existing == null)
        {
            return false;
        }

        ValidateReferences(existing.DeviceId, existing.CommandCatalogueId);

        existing.IsActive = true;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateDeviceCapability(id, existing);
    }

    private void ValidateReferences(int deviceId, int commandCatalogueId)
    {
        var device = _deviceDataAccess.GetDeviceById(deviceId);
        if (device == null || !device.IsActive)
        {
            throw new ArgumentException("Device must exist and be active.");
        }

        var command = _commandCatalogueDataAccess.GetCommandCatalogueById(commandCatalogueId);
        if (command == null || !command.IsActive)
        {
            throw new ArgumentException("CommandCatalogue must exist and be active.");
        }
    }

    private void EnsurePairUnique(int deviceId, int commandCatalogueId, int? currentId)
    {
        if (_dataAccess.DeviceCapabilityExists(deviceId, commandCatalogueId, currentId))
        {
            throw new InvalidOperationException("This device capability already exists.");
        }
    }

    private static DeviceCapabilityResponse MapToResponse(DeviceCapability model)
    {
        return new DeviceCapabilityResponse
        {
            Id = model.Id,
            DeviceId = model.DeviceId,
            CommandCatalogueId = model.CommandCatalogueId,
            RequiresMap = model.RequiresMap,
            Description = model.Description,
            IsActive = model.IsActive,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };
    }
}
