using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Dtos;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Services;

namespace RobotControllerApi.BoundedContexts.DeviceStatuses.Controllers;

[ApiController]
[Route("api/device-status")]
public class DeviceStatusController : ControllerBase
{
    private readonly IDeviceStatusService _service;

    public DeviceStatusController(IDeviceStatusService service)
    {
        _service = service;
    }

    [Authorize(Policy = AuthorizationPolicies.HumanUser)]
    [HttpGet]
    public ActionResult GetDeviceStatuses()
    {
        return Ok(_service.GetDeviceStatuses());
    }

    [Authorize(Policy = AuthorizationPolicies.HumanUser)]
    [HttpGet("{id}")]
    public ActionResult GetDeviceStatusById(int id)
    {
        var response = _service.GetDeviceStatusById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.HumanUser)]
    [HttpGet("/api/devices/{deviceId}/status")]
    public ActionResult GetDeviceStatusByDeviceId(int deviceId)
    {
        var response = _service.GetDeviceStatusByDeviceId(deviceId);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.DeviceAdapter)]
    [HttpPut("/api/devices/{deviceId}/status")]
    public ActionResult UpdateDeviceStatus(int deviceId, UpdateDeviceStatusRequest request)
    {
        try
        {
            var success = _service.UpdateDeviceStatus(deviceId, request);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.DeviceAdapter)]
    [HttpPatch("/api/devices/{deviceId}/status/heartbeat")]
    public ActionResult RecordHeartbeat(int deviceId, HeartbeatRequest request)
    {
        try
        {
            var success = _service.RecordHeartbeat(deviceId, request);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.DeviceAdapter)]
    [HttpPatch("/api/devices/{deviceId}/status/grid-pose")]
    public ActionResult UpdateGridPose(int deviceId, UpdateGridPoseRequest request)
    {
        try
        {
            var success = _service.UpdateGridPose(deviceId, request);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.DeviceAdapter)]
    [HttpPatch("/api/devices/{deviceId}/status/invalidate-grid-pose")]
    public ActionResult InvalidateGridPose(int deviceId, InvalidateGridPoseRequest request)
    {
        try
        {
            var success = _service.InvalidateGridPose(deviceId, request);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

}
