using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Auth.Services;
using RobotControllerApi.BoundedContexts.Workflows.Dtos;
using RobotControllerApi.BoundedContexts.Workflows.Services;

namespace RobotControllerApi.BoundedContexts.Workflows.Controllers;

[ApiController]
[Route("api/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _service;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public WorkflowsController(IWorkflowService service, CurrentUserAccessor currentUserAccessor)
    {
        _service = service;
        _currentUserAccessor = currentUserAccessor;
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpGet]
    public ActionResult GetWorkflows() => Ok(_service.GetWorkflows());

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpGet("{id}")]
    public ActionResult GetWorkflowById(int id)
    {
        var response = _service.GetWorkflowWithJobsById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpGet("/api/devices/{deviceId}/workflows")]
    public ActionResult GetWorkflowsByDeviceId(int deviceId) => Ok(_service.GetWorkflowsByDeviceId(deviceId));

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPost("/api/devices/{deviceId}/workflows")]
    public ActionResult CreateWorkflow(int deviceId, CreateWorkflowRequest request)
    {
        try
        {
            var appUserId = _currentUserAccessor.GetRequiredAppUserId(User);
            var response = _service.CreateWorkflow(deviceId, request, appUserId);
            return CreatedAtAction(nameof(GetWorkflowById), new { id = response.Id }, response);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPut("{id}")]
    public ActionResult UpdateWorkflow(int id, UpdateWorkflowRequest request)
    {
        try
        {
            var success = _service.UpdateWorkflow(id, request);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPatch("{id}/cancel")]
    public ActionResult CancelWorkflow(int id)
    {
        try
        {
            var success = _service.CancelWorkflow(id);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpDelete("{id}")]
    public ActionResult DeleteWorkflow(int id)
    {
        try
        {
            var success = _service.DeleteWorkflow(id);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.DeviceCredential)]
    [HttpPatch("/api/adapter/workflows/{workflowId}/started")]
    public ActionResult MarkWorkflowStarted(int workflowId)
    {
        try
        {
            var credentialId = _currentUserAccessor.GetRequiredDeviceCredentialId(User);
            var success = _service.MarkWorkflowStarted(workflowId, credentialId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.DeviceCredential)]
    [HttpPatch("/api/adapter/workflows/{workflowId}/completed")]
    public ActionResult MarkWorkflowCompleted(int workflowId, CompleteWorkflowRequest request)
    {
        try
        {
            var credentialId = _currentUserAccessor.GetRequiredDeviceCredentialId(User);
            var success = _service.MarkWorkflowCompleted(workflowId, request, credentialId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.DeviceCredential)]
    [HttpPatch("/api/adapter/workflows/{workflowId}/failed")]
    public ActionResult MarkWorkflowFailed(int workflowId, FailWorkflowRequest request)
    {
        try
        {
            var credentialId = _currentUserAccessor.GetRequiredDeviceCredentialId(User);
            var success = _service.MarkWorkflowFailed(workflowId, request, credentialId);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}
