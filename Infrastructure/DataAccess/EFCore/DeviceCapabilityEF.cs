using RobotControllerApi.BoundedContexts.DeviceCapabilities.Models;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class DeviceCapabilityEF : IDeviceCapabilityDataAccess
{
    private readonly RobotContext _context;

    public DeviceCapabilityEF(RobotContext context)
    {
        _context = context;
    }

    public List<DeviceCapability> GetDeviceCapabilities()
    {
        return _context.DeviceCapabilities
            .OrderBy(x => x.Id)
            .ToList();
    }

    public DeviceCapability? GetDeviceCapabilityById(int id)
    {
        return _context.DeviceCapabilities
            .SingleOrDefault(x => x.Id == id);
    }

    public List<DeviceCapability> GetDeviceCapabilitiesByDeviceId(int deviceId)
    {
        return _context.DeviceCapabilities
            .Where(x => x.DeviceId == deviceId)
            .OrderBy(x => x.Id)
            .ToList();
    }

    public bool DeviceCapabilityExists(int deviceId, int commandCatalogueId, int? excludeId = null)
    {
        var query = _context.DeviceCapabilities
            .Where(x => x.DeviceId == deviceId && x.CommandCatalogueId == commandCatalogueId);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return query.Any();
    }

    public DeviceCapability InsertDeviceCapability(DeviceCapability newDeviceCapability)
    {
        _context.DeviceCapabilities.Add(newDeviceCapability);
        _context.SaveChanges();
        return newDeviceCapability;
    }

    public bool UpdateDeviceCapability(int id, DeviceCapability updatedDeviceCapability)
    {
        var existingDeviceCapability = _context.DeviceCapabilities
            .SingleOrDefault(x => x.Id == id);

        if (existingDeviceCapability == null)
        {
            return false;
        }

        existingDeviceCapability.DeviceId = updatedDeviceCapability.DeviceId;
        existingDeviceCapability.CommandCatalogueId = updatedDeviceCapability.CommandCatalogueId;
        existingDeviceCapability.RequiresMap = updatedDeviceCapability.RequiresMap;
        existingDeviceCapability.Description = updatedDeviceCapability.Description;
        existingDeviceCapability.IsActive = updatedDeviceCapability.IsActive;
        existingDeviceCapability.ModifiedDate = updatedDeviceCapability.ModifiedDate;

        _context.SaveChanges();
        return true;
    }

    public bool DeleteDeviceCapability(int id)
    {
        var existingDeviceCapability = _context.DeviceCapabilities
            .SingleOrDefault(x => x.Id == id);

        if (existingDeviceCapability == null)
        {
            return false;
        }

        _context.DeviceCapabilities.Remove(existingDeviceCapability);
        _context.SaveChanges();
        return true;
    }
}
