using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Dtos;
using RobotControllerApi.BoundedContexts.DeviceCapabilities.Services;

namespace RobotControllerApi.BoundedContexts.DeviceCapabilities.Controllers;

[ApiController]
[Route("api/device-capabilities")]
[Authorize(Policy = AuthorizationPolicies.HumanUser)]
public class DeviceCapabilityController : ControllerBase
{
    private readonly IDeviceCapabilityService _service;

    public DeviceCapabilityController(IDeviceCapabilityService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetDeviceCapabilities()
    {
        return Ok(_service.GetDeviceCapabilities());
    }

    [HttpGet("{id}")]
    public ActionResult GetDeviceCapabilityById(int id)
    {
        var response = _service.GetDeviceCapabilityById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpGet("/api/devices/{deviceId}/capabilities")]
    public ActionResult GetDeviceCapabilitiesByDeviceId(int deviceId)
    {
        return Ok(_service.GetDeviceCapabilitiesByDeviceId(deviceId));
    }

    [HttpPost]
    public ActionResult CreateDeviceCapability(CreateDeviceCapabilityRequest request)
    {
        try
        {
            var response = _service.CreateDeviceCapability(request);
            return CreatedAtAction(nameof(GetDeviceCapabilityById), new { id = response.Id }, response);
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

    [HttpPut("{id}")]
    public ActionResult UpdateDeviceCapability(int id, UpdateDeviceCapabilityRequest request)
    {
        try
        {
            var success = _service.UpdateDeviceCapability(id, request);
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

    [HttpDelete("{id}")]
    public ActionResult DeleteDeviceCapability(int id)
    {
        try
        {
            var success = _service.DeleteDeviceCapability(id);
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

    [HttpPatch("{id}/deactivate")]
    public ActionResult DeactivateDeviceCapability(int id)
    {
        try
        {
            var success = _service.DeactivateDeviceCapability(id);
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

    [HttpPatch("{id}/reactivate")]
    public ActionResult ReactivateDeviceCapability(int id)
    {
        try
        {
            var success = _service.ReactivateDeviceCapability(id);
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
