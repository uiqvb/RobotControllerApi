using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.AppUsers.Dtos;
using RobotControllerApi.BoundedContexts.AppUsers.Services;

namespace RobotControllerApi.BoundedContexts.AppUsers.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic, Roles = "Admin")]
[Route("api/users")]
public class AppUserController : ControllerBase
{
    private readonly IAppUserService _service;

    public AppUserController(IAppUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetAppUsers()
    {
        return Ok(_service.GetAppUsers());
    }

    [HttpGet("{id}")]
    public ActionResult GetAppUserById(int id)
    {
        var response = _service.GetAppUserById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpPost]
    public ActionResult CreateAppUser(CreateAppUserRequest request)
    {
        try
        {
            var response = _service.CreateAppUser(request);
            return CreatedAtAction(nameof(GetAppUserById), new { id = response.Id }, response);
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
    public ActionResult UpdateAppUser(int id, UpdateAppUserRequest request)
    {
        try
        {
            var success = _service.UpdateAppUser(id, request);
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
    public ActionResult DeactivateAppUser(int id)
    {
        try
        {
            var success = _service.DeactivateAppUser(id);
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
    public ActionResult ReactivateAppUser(int id)
    {
        try
        {
            var success = _service.ReactivateAppUser(id);
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
    public ActionResult DeleteAppUser(int id)
    {
        var success = _service.DeleteAppUser(id);
        if (!success) return NotFound();
        return NoContent();
    }
}