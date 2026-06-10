using RobotControllerApi.BoundedContexts.LiveControls.Models;

namespace RobotControllerApi.BoundedContexts.LiveControls.Persistence;

public interface ILiveControlCommandDataAccess
{
    LiveControlCommand? GetLiveControlCommandById(int id);
    LiveControlCommand? GetLiveControlCommandByDeviceId(int deviceId);
    LiveControlCommand InsertLiveControlCommand(LiveControlCommand newLiveControlCommand);
    bool UpdateLiveControlCommand(int id, LiveControlCommand updatedLiveControlCommand);
    bool DeleteLiveControlCommand(int id);
}
