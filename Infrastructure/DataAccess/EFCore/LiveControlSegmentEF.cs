using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class LiveControlSegmentEF : ILiveControlSegmentDataAccess
{
    private readonly RobotContext _context;

    public LiveControlSegmentEF(RobotContext context)
    {
        _context = context;
    }

    public LiveControlSegment? GetLiveControlSegmentById(int id) => _context.LiveControlSegments.FirstOrDefault(x => x.Id == id);

    public List<LiveControlSegment> GetLiveControlSegmentsBySessionId(int liveControlSessionId)
    {
        return _context.LiveControlSegments
            .Where(x => x.LiveControlSessionId == liveControlSessionId)
            .OrderBy(x => x.StartedAtUtc)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public LiveControlSegment InsertLiveControlSegment(LiveControlSegment newLiveControlSegment)
    {
        _context.LiveControlSegments.Add(newLiveControlSegment);
        _context.SaveChanges();
        return newLiveControlSegment;
    }

    public bool DeleteLiveControlSegment(int id)
    {
        var existing = _context.LiveControlSegments.FirstOrDefault(x => x.Id == id);
        if (existing == null) return false;
        _context.LiveControlSegments.Remove(existing);
        _context.SaveChanges();
        return true;
    }
}
