using RobotControllerApi.BoundedContexts.LiveControls.Models;

namespace RobotControllerApi.BoundedContexts.LiveControls.Persistence;

public interface ILiveControlSessionDataAccess
{
    LiveControlSession? GetLiveControlSessionById(int id);
    List<LiveControlSession> GetLiveControlSessionsByDeviceId(int deviceId);
    LiveControlSession? GetActiveLiveControlSessionByDeviceId(int deviceId);
    LiveControlSession InsertLiveControlSession(LiveControlSession newLiveControlSession);
    bool UpdateLiveControlSession(int id, LiveControlSession updatedLiveControlSession);
    bool DeleteLiveControlSession(int id);
}
