using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Dtos;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Services;

namespace RobotControllerApi.BoundedContexts.WorkflowHistories.Controllers;

[ApiController]
[Route("api/workflow-history")]
[Authorize(Policy = AuthorizationPolicies.HumanUser)]
public class WorkflowHistoryController : ControllerBase
{
    private readonly IWorkflowHistoryService _service;

    public WorkflowHistoryController(IWorkflowHistoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetWorkflowHistories() => Ok(_service.GetWorkflowHistories());

    [HttpGet("{id}")]
    public ActionResult GetWorkflowHistoryById(int id)
    {
        var response = _service.GetWorkflowHistoryById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpGet("/api/workflows/{workflowId}/history")]
    public ActionResult GetWorkflowHistoriesByWorkflowId(int workflowId) => Ok(_service.GetWorkflowHistoriesByWorkflowId(workflowId));

    [HttpGet("/api/devices/{deviceId}/workflow-history")]
    public ActionResult GetWorkflowHistoriesByDeviceId(int deviceId) => Ok(_service.GetWorkflowHistoriesByDeviceId(deviceId));

    [HttpPost]
    public ActionResult CreateWorkflowHistory(CreateWorkflowHistoryRequest request)
    {
        try
        {
            var response = _service.CreateWorkflowHistory(request);
            return CreatedAtAction(nameof(GetWorkflowHistoryById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("external")]
    public ActionResult CreateExternalWorkflowHistory(CreateExternalWorkflowHistoryRequest request)
    {
        try
        {
            var response = _service.CreateExternalWorkflowHistory(request);
            return CreatedAtAction(nameof(GetWorkflowHistoryById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}
