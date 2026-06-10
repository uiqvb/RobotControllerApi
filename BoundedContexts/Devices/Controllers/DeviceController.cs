using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Devices.Dtos;
using RobotControllerApi.BoundedContexts.Devices.Services;

namespace RobotControllerApi.BoundedContexts.Devices.Controllers;

[ApiController]
[Route("api/devices")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _service;

    public DeviceController(IDeviceService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetDevices()
    {
        return Ok(_service.GetDevices());
    }

    [HttpGet("{id}")]
    public ActionResult GetDeviceById(int id)
    {
        var response = _service.GetDeviceById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpPost]
    public ActionResult CreateDevice(CreateDeviceRequest request)
    {
        try
        {
            var response = _service.CreateDevice(request);
            return CreatedAtAction(nameof(GetDeviceById), new { id = response.Id }, response);
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
    public ActionResult UpdateDevice(int id, UpdateDeviceRequest request)
    {
        try
        {
            var success = _service.UpdateDevice(id, request);
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
    public ActionResult DeleteDevice(int id)
    {
        try
        {
            var success = _service.DeleteDevice(id);
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

    [HttpPatch("{id}/assign-map")]
    public ActionResult AssignMap(int id, AssignMapRequest request)
    {
        try
        {
            var success = _service.AssignMap(id, request);
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

    [HttpPatch("{id}/unassign-map")]
    public ActionResult UnassignMap(int id)
    {
        try
        {
            var success = _service.UnassignMap(id);
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
    public ActionResult DeactivateDevice(int id)
    {
        try
        {
            var success = _service.DeactivateDevice(id);
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
    public ActionResult ReactivateDevice(int id)
    {
        try
        {
            var success = _service.ReactivateDevice(id);
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
