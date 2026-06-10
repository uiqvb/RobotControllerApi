using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class LiveControlCommandEF : ILiveControlCommandDataAccess
{
    private readonly RobotContext _context;

    public LiveControlCommandEF(RobotContext context)
    {
        _context = context;
    }

    public LiveControlCommand? GetLiveControlCommandById(int id) => _context.LiveControlCommands.FirstOrDefault(x => x.Id == id);

    public LiveControlCommand? GetLiveControlCommandByDeviceId(int deviceId)
    {
        return _context.LiveControlCommands
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.ModifiedDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    public LiveControlCommand InsertLiveControlCommand(LiveControlCommand newLiveControlCommand)
    {
        _context.LiveControlCommands.Add(newLiveControlCommand);
        _context.SaveChanges();
        return newLiveControlCommand;
    }

    public bool UpdateLiveControlCommand(int id, LiveControlCommand updatedLiveControlCommand)
    {
        var existing = _context.LiveControlCommands.FirstOrDefault(x => x.Id == id);
        if (existing == null) return false;

        existing.DeviceId = updatedLiveControlCommand.DeviceId;
        existing.AppUserId = updatedLiveControlCommand.AppUserId;
        existing.LiveControlSessionId = updatedLiveControlCommand.LiveControlSessionId;
        existing.CommandName = updatedLiveControlCommand.CommandName;
        existing.PayloadJson = updatedLiveControlCommand.PayloadJson;
        existing.SequenceNumber = updatedLiveControlCommand.SequenceNumber;
        existing.ExpiresAtUtc = updatedLiveControlCommand.ExpiresAtUtc;
        existing.ModifiedDate = updatedLiveControlCommand.ModifiedDate;
        _context.SaveChanges();
        return true;
    }

    public bool DeleteLiveControlCommand(int id)
    {
        var existing = _context.LiveControlCommands.FirstOrDefault(x => x.Id == id);
        if (existing == null) return false;
        _context.LiveControlCommands.Remove(existing);
        _context.SaveChanges();
        return true;
    }
}
