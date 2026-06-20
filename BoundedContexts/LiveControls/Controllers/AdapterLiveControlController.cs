using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Auth.Services;
using RobotControllerApi.BoundedContexts.LiveControls.Dtos;
using RobotControllerApi.BoundedContexts.LiveControls.Services;

namespace RobotControllerApi.BoundedContexts.LiveControls.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.DeviceAdapter)]
[Route("api/adapter/devices/{deviceId:int}")]
public class AdapterLiveControlController : ControllerBase
{
    private readonly ILiveControlService _service;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public AdapterLiveControlController(ILiveControlService service, CurrentUserAccessor currentUserAccessor)
    {
        _service = service;
        _currentUserAccessor = currentUserAccessor;
    }

    [HttpGet("live-control-command")]
    public ActionResult GetLatestLiveControlCommand(int deviceId)
    {
        try
        {
            var authenticatedDeviceId = _currentUserAccessor.GetRequiredDeviceId(User);
            return Ok(_service.GetLatestLiveControlCommandForAdapter(deviceId, authenticatedDeviceId));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("live-control-sessions/{sessionId:int}/segments")]
    public ActionResult CreateLiveControlSegment(int deviceId, int sessionId, CreateLiveControlSegmentRequest request)
    {
        try
        {
            var authenticatedDeviceId = _currentUserAccessor.GetRequiredDeviceId(User);
            var response = _service.CreateLiveControlSegment(deviceId, sessionId, request, authenticatedDeviceId);
            return CreatedAtAction(nameof(CreateLiveControlSegment), new { deviceId, sessionId }, response);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}
