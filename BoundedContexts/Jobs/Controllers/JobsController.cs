using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Auth.Services;
using RobotControllerApi.BoundedContexts.Jobs.Dtos;
using RobotControllerApi.BoundedContexts.Jobs.Services;

namespace RobotControllerApi.BoundedContexts.Jobs.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IJobService _service;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public JobsController(IJobService service, CurrentUserAccessor currentUserAccessor)
    {
        _service = service;
        _currentUserAccessor = currentUserAccessor;
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpGet]
    public ActionResult GetJobs() => Ok(_service.GetJobs());

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpGet("{id}")]
    public ActionResult GetJobById(int id)
    {
        var response = _service.GetJobById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpGet("/api/devices/{deviceId}/jobs")]
    public ActionResult GetJobsByDeviceId(int deviceId) => Ok(_service.GetJobsByDeviceId(deviceId));

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPost("/api/devices/{deviceId}/jobs")]
    public ActionResult CreateJob(int deviceId, CreateJobRequest request)
    {
        try
        {
            var appUserId = _currentUserAccessor.GetRequiredAppUserId(User);
            var response = _service.CreateJob(deviceId, request, appUserId);
            return CreatedAtAction(nameof(GetJobById), new { id = response.Id }, response);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPut("{id}")]
    public ActionResult UpdateJob(int id, UpdateJobRequest request)
    {
        try
        {
            var success = _service.UpdateJob(id, request);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPatch("{id}/cancel")]
    public ActionResult CancelJob(int id)
    {
        try
        {
            var success = _service.CancelJob(id);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPatch("{id}/deactivate")]
    public ActionResult DeactivateJob(int id) => CancelJob(id);

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpDelete("{id}")]
    public ActionResult DeleteJob(int id)
    {
        try
        {
            var success = _service.DeleteJob(id);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.DeviceCredential)]
    [HttpPatch("/api/adapter/jobs/{jobId}/started")]
    public ActionResult MarkJobStarted(int jobId)
    {
        try
        {
            var credentialId = _currentUserAccessor.GetRequiredDeviceCredentialId(User);
            var success = _service.MarkJobStarted(jobId, credentialId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.DeviceCredential)]
    [HttpPatch("/api/adapter/jobs/{jobId}/completed")]
    public ActionResult MarkJobCompleted(int jobId, CompleteJobRequest request)
    {
        try
        {
            var credentialId = _currentUserAccessor.GetRequiredDeviceCredentialId(User);
            var success = _service.MarkJobCompleted(jobId, request, credentialId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.DeviceCredential)]
    [HttpPatch("/api/adapter/jobs/{jobId}/failed")]
    public ActionResult MarkJobFailed(int jobId, FailJobRequest request)
    {
        try
        {
            var credentialId = _currentUserAccessor.GetRequiredDeviceCredentialId(User);
            var success = _service.MarkJobFailed(jobId, request, credentialId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}
