using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Dtos;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Services;

namespace RobotControllerApi.BoundedContexts.CommandCatalogues.Controllers;

[ApiController]
[Route("api/command-catalogue")]
public class CommandCatalogueController : ControllerBase
{
    private readonly ICommandCatalogueService _service;

    public CommandCatalogueController(ICommandCatalogueService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetCommandCatalogues()
    {
        return Ok(_service.GetCommandCatalogues());
    }

    [HttpGet("{id}")]
    public ActionResult GetCommandCatalogueById(int id)
    {
        var response = _service.GetCommandCatalogueById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpPost]
    public ActionResult CreateCommandCatalogue(CreateCommandCatalogueRequest request)
    {
        try
        {
            var response = _service.CreateCommandCatalogue(request);
            return CreatedAtAction(nameof(GetCommandCatalogueById), new { id = response.Id }, response);
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
    public ActionResult UpdateCommandCatalogue(int id, UpdateCommandCatalogueRequest request)
    {
        try
        {
            var success = _service.UpdateCommandCatalogue(id, request);
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
    public ActionResult DeleteCommandCatalogue(int id)
    {
        try
        {
            var success = _service.DeleteCommandCatalogue(id);
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
    public ActionResult DeactivateCommandCatalogue(int id)
    {
        try
        {
            var success = _service.DeactivateCommandCatalogue(id);
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
    public ActionResult ReactivateCommandCatalogue(int id)
    {
        try
        {
            var success = _service.ReactivateCommandCatalogue(id);
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
