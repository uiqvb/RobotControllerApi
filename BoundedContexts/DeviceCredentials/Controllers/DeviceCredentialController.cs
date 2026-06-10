using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Dtos;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Services;

namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
[Route("api/device-credentials")]
public class DeviceCredentialController : ControllerBase
{
    private readonly IDeviceCredentialService _service;

    public DeviceCredentialController(IDeviceCredentialService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetDeviceCredentials()
    {
        return Ok(_service.GetDeviceCredentials());
    }

    [HttpGet("{id}")]
    public ActionResult GetDeviceCredentialById(int id)
    {
        var response = _service.GetDeviceCredentialById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpGet("/api/devices/{deviceId}/credentials")]
    public ActionResult GetDeviceCredentialsByDeviceId(int deviceId)
    {
        return Ok(_service.GetDeviceCredentialsByDeviceId(deviceId));
    }

    [HttpPost("/api/devices/{deviceId}/credentials")]
    public ActionResult CreateDeviceCredential(int deviceId, CreateDeviceCredentialRequest request)
    {
        try
        {
            var response = _service.CreateDeviceCredential(deviceId, request);
            return CreatedAtAction(nameof(GetDeviceCredentialById), new { id = response.Id }, response);
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
    public ActionResult DeactivateDeviceCredential(int id)
    {
        try
        {
            var success = _service.DeactivateDeviceCredential(id);
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
    public ActionResult ReactivateDeviceCredential(int id)
    {
        try
        {
            var success = _service.ReactivateDeviceCredential(id);
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

    [HttpPatch("{id}/revoke")]
    public ActionResult RevokeDeviceCredential(int id, RevokeDeviceCredentialRequest request)
    {
        try
        {
            var success = _service.RevokeDeviceCredential(id, request);
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
    public ActionResult DeleteDeviceCredential(int id)
    {
        try
        {
            var success = _service.DeleteDeviceCredential(id);
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
