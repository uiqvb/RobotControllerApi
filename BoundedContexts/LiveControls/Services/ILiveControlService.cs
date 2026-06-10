using RobotControllerApi.BoundedContexts.LiveControls.Dtos;

namespace RobotControllerApi.BoundedContexts.LiveControls.Services;

public interface ILiveControlService
{
    LiveControlSessionResponse StartLiveControlSession(int deviceId, StartLiveControlSessionRequest request, int appUserId);
    bool StopLiveControlSession(int deviceId, int sessionId, StopLiveControlSessionRequest request, int appUserId);
    List<LiveControlSessionResponse> GetLiveControlSessionsByDeviceId(int deviceId);
    LiveControlSessionResponse? GetLiveControlSessionById(int id);

    LiveControlCommandResponse SetLiveControlCommand(int deviceId, SetLiveControlCommandRequest request, int appUserId);
    LiveControlCommandResponse GetLatestLiveControlCommandForAdapter(int deviceId, int authenticatedDeviceId);

    LiveControlSegmentResponse CreateLiveControlSegment(int deviceId, int sessionId, CreateLiveControlSegmentRequest request, int authenticatedDeviceId);
    List<LiveControlSegmentResponse> GetLiveControlSegmentsBySessionId(int sessionId);
}
