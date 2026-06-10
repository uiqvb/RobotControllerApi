using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Auth.Services;
using RobotControllerApi.BoundedContexts.LiveControls.Dtos;
using RobotControllerApi.BoundedContexts.LiveControls.Services;

namespace RobotControllerApi.BoundedContexts.LiveControls.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
[Route("api")]
public class LiveControlController : ControllerBase
{
    private readonly ILiveControlService _service;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public LiveControlController(ILiveControlService service, CurrentUserAccessor currentUserAccessor)
    {
        _service = service;
        _currentUserAccessor = currentUserAccessor;
    }

    [HttpPost("devices/{deviceId:int}/live-control-sessions/start")]
    public ActionResult StartLiveControlSession(int deviceId, StartLiveControlSessionRequest request)
    {
        try
        {
            var appUserId = _currentUserAccessor.GetRequiredAppUserId(User);
            var response = _service.StartLiveControlSession(deviceId, request, appUserId);
            return CreatedAtAction(nameof(GetLiveControlSessionById), new { sessionId = response.Id }, response);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("devices/{deviceId:int}/live-control-sessions/{sessionId:int}/stop")]
    public ActionResult StopLiveControlSession(int deviceId, int sessionId, StopLiveControlSessionRequest request)
    {
        try
        {
            var appUserId = _currentUserAccessor.GetRequiredAppUserId(User);
            var success = _service.StopLiveControlSession(deviceId, sessionId, request, appUserId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpGet("devices/{deviceId:int}/live-control-sessions")]
    public ActionResult GetLiveControlSessionsByDeviceId(int deviceId)
    {
        try
        {
            return Ok(_service.GetLiveControlSessionsByDeviceId(deviceId));
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("live-control-sessions/{sessionId:int}")]
    public ActionResult GetLiveControlSessionById(int sessionId)
    {
        var response = _service.GetLiveControlSessionById(sessionId);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpPut("devices/{deviceId:int}/live-control-command")]
    public ActionResult SetLiveControlCommand(int deviceId, SetLiveControlCommandRequest request)
    {
        try
        {
            var appUserId = _currentUserAccessor.GetRequiredAppUserId(User);
            return Ok(_service.SetLiveControlCommand(deviceId, request, appUserId));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpGet("live-control-sessions/{sessionId:int}/segments")]
    public ActionResult GetLiveControlSegmentsBySessionId(int sessionId)
    {
        try
        {
            return Ok(_service.GetLiveControlSegmentsBySessionId(sessionId));
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
