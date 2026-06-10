using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.DevicePermissions.Dtos;
using RobotControllerApi.BoundedContexts.DevicePermissions.Services;

namespace RobotControllerApi.BoundedContexts.DevicePermissions.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
[Route("api/device-permissions")]
public class DevicePermissionController : ControllerBase
{
    private readonly IDevicePermissionService _service;

    public DevicePermissionController(IDevicePermissionService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetDevicePermissions()
    {
        return Ok(_service.GetDevicePermissions());
    }

    [HttpGet("{id}")]
    public ActionResult GetDevicePermissionById(int id)
    {
        var response = _service.GetDevicePermissionById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpGet("/api/users/{userId}/device-permissions")]
    public ActionResult GetDevicePermissionsByUserId(int userId)
    {
        return Ok(_service.GetDevicePermissionsByUserId(userId));
    }

    [HttpGet("/api/devices/{deviceId}/permissions")]
    public ActionResult GetDevicePermissionsByDeviceId(int deviceId)
    {
        return Ok(_service.GetDevicePermissionsByDeviceId(deviceId));
    }

    [HttpPost]
    public ActionResult CreateDevicePermission(CreateDevicePermissionRequest request)
    {
        try
        {
            var response = _service.CreateDevicePermission(request);
            return CreatedAtAction(nameof(GetDevicePermissionById), new { id = response.Id }, response);
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
    public ActionResult UpdateDevicePermission(int id, UpdateDevicePermissionRequest request)
    {
        try
        {
            var success = _service.UpdateDevicePermission(id, request);
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
    public ActionResult DeactivateDevicePermission(int id)
    {
        try
        {
            var success = _service.DeactivateDevicePermission(id);
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
    public ActionResult ReactivateDevicePermission(int id)
    {
        try
        {
            var success = _service.ReactivateDevicePermission(id);
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
    public ActionResult DeleteDevicePermission(int id)
    {
        try
        {
            var success = _service.DeleteDevicePermission(id);
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
