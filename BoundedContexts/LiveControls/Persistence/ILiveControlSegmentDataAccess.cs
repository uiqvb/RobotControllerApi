using RobotControllerApi.BoundedContexts.LiveControls.Models;

namespace RobotControllerApi.BoundedContexts.LiveControls.Persistence;

public interface ILiveControlSegmentDataAccess
{
    LiveControlSegment? GetLiveControlSegmentById(int id);
    List<LiveControlSegment> GetLiveControlSegmentsBySessionId(int liveControlSessionId);
    LiveControlSegment InsertLiveControlSegment(LiveControlSegment newLiveControlSegment);
    bool DeleteLiveControlSegment(int id);
}
