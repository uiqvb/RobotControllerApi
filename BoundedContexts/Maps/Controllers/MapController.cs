using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Maps.Dtos;
using RobotControllerApi.BoundedContexts.Maps.Services;

namespace RobotControllerApi.BoundedContexts.Maps.Controllers;

[ApiController]
[Route("api/maps")]
[Authorize(Policy = AuthorizationPolicies.HumanUser)]
public class MapController : ControllerBase
{
    private readonly IMapService _service;

    public MapController(IMapService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetMaps()
    {
        return Ok(_service.GetMaps());
    }

    [HttpGet("{id}")]
    public ActionResult GetMapById(int id)
    {
        var response = _service.GetMapById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpPost]
    public ActionResult CreateMap(CreateMapRequest request)
    {
        try
        {
            var response = _service.CreateMap(request);
            return CreatedAtAction(nameof(GetMapById), new { id = response.Id }, response);
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
    public ActionResult UpdateMap(int id, UpdateMapRequest request)
    {
        try
        {
            var success = _service.UpdateMap(id, request);
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
    public ActionResult DeleteMap(int id)
    {
        try
        {
            var success = _service.DeleteMap(id);
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
    public ActionResult DeactivateMap(int id)
    {
        try
        {
            var success = _service.DeactivateMap(id);
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
    public ActionResult ReactivateMap(int id)
    {
        try
        {
            var success = _service.ReactivateMap(id);
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
