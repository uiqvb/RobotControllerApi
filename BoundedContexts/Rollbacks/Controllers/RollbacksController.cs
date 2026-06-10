using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Rollbacks.Dtos;
using RobotControllerApi.BoundedContexts.Rollbacks.Services;

namespace RobotControllerApi.BoundedContexts.Rollbacks.Controllers;

[ApiController]
[Route("api/rollback-requests")]
public class RollbacksController : ControllerBase
{
    private readonly IRollbackService _service;

    public RollbacksController(IRollbackService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetRollbackRequests() => Ok(_service.GetRollbackRequests());

    [HttpGet("{id}")]
    public ActionResult GetRollbackRequestById(int id)
    {
        var response = _service.GetRollbackRequestById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpGet("/api/devices/{deviceId}/rollback-requests")]
    public ActionResult GetRollbackRequestsByDeviceId(int deviceId) => Ok(_service.GetRollbackRequestsByDeviceId(deviceId));

    [HttpPost("/api/jobs/{jobId}/rollback")]
    public ActionResult GenerateRollbackForJob(int jobId, CreateRollbackFromJobHistoryRequest request)
    {
        try { return Ok(_service.GenerateRollbackForJob(jobId, request)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("/api/workflows/{workflowId}/rollback")]
    public ActionResult GenerateRollbackForWorkflow(int workflowId, CreateRollbackFromWorkflowHistoryRequest request)
    {
        try { return Ok(_service.GenerateRollbackForWorkflow(workflowId, request)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("/api/rollback/job-history")]
    public ActionResult GenerateRollbackFromJobHistories(CreateRollbackFromJobHistoryRequest request)
    {
        try { return Ok(_service.GenerateRollbackFromJobHistories(request)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("/api/rollback/workflow-history")]
    public ActionResult GenerateRollbackFromWorkflowHistories(CreateRollbackFromWorkflowHistoryRequest request)
    {
        try { return Ok(_service.GenerateRollbackFromWorkflowHistories(request)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}
