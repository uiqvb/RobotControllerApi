using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class LiveControlSessionEF : ILiveControlSessionDataAccess
{
    private readonly RobotContext _context;

    public LiveControlSessionEF(RobotContext context)
    {
        _context = context;
    }

    public LiveControlSession? GetLiveControlSessionById(int id) => _context.LiveControlSessions.FirstOrDefault(x => x.Id == id);

    public List<LiveControlSession> GetLiveControlSessionsByDeviceId(int deviceId)
    {
        return _context.LiveControlSessions
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.StartedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    public LiveControlSession? GetActiveLiveControlSessionByDeviceId(int deviceId)
    {
        return _context.LiveControlSessions
            .Where(x => x.DeviceId == deviceId && x.Status == "Active")
            .OrderByDescending(x => x.StartedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    public LiveControlSession InsertLiveControlSession(LiveControlSession newLiveControlSession)
    {
        _context.LiveControlSessions.Add(newLiveControlSession);
        _context.SaveChanges();
        return newLiveControlSession;
    }

    public bool UpdateLiveControlSession(int id, LiveControlSession updatedLiveControlSession)
    {
        var existing = _context.LiveControlSessions.FirstOrDefault(x => x.Id == id);
        if (existing == null) return false;

        existing.DeviceId = updatedLiveControlSession.DeviceId;
        existing.AppUserId = updatedLiveControlSession.AppUserId;
        existing.Status = updatedLiveControlSession.Status;
        existing.StopReason = updatedLiveControlSession.StopReason;
        existing.IsRollback = updatedLiveControlSession.IsRollback;
        existing.RollbackOfLiveControlSessionId = updatedLiveControlSession.RollbackOfLiveControlSessionId;
        existing.StartedAtUtc = updatedLiveControlSession.StartedAtUtc;
        existing.EndedAtUtc = updatedLiveControlSession.EndedAtUtc;
        existing.ModifiedDate = updatedLiveControlSession.ModifiedDate;
        _context.SaveChanges();
        return true;
    }

    public bool DeleteLiveControlSession(int id)
    {
        var existing = _context.LiveControlSessions.FirstOrDefault(x => x.Id == id);
        if (existing == null) return false;
        _context.LiveControlSessions.Remove(existing);
        _context.SaveChanges();
        return true;
    }
}
