using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.JobHistories.Dtos;
using RobotControllerApi.BoundedContexts.JobHistories.Services;

namespace RobotControllerApi.BoundedContexts.JobHistories.Controllers;

[ApiController]
[Route("api/job-history")]
public class JobHistoryController : ControllerBase
{
    private readonly IJobHistoryService _service;

    public JobHistoryController(IJobHistoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult GetJobHistories() => Ok(_service.GetJobHistories());

    [HttpGet("{id}")]
    public ActionResult GetJobHistoryById(int id)
    {
        var response = _service.GetJobHistoryById(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpGet("/api/jobs/{jobId}/history")]
    public ActionResult GetJobHistoriesByJobId(int jobId) => Ok(_service.GetJobHistoriesByJobId(jobId));

    [HttpGet("/api/workflows/{workflowId}/job-history")]
    public ActionResult GetJobHistoriesByWorkflowId(int workflowId) => Ok(_service.GetJobHistoriesByWorkflowId(workflowId));

    [HttpGet("/api/devices/{deviceId}/job-history")]
    public ActionResult GetJobHistoriesByDeviceId(int deviceId) => Ok(_service.GetJobHistoriesByDeviceId(deviceId));

    [HttpPost]
    public ActionResult CreateJobHistory(CreateJobHistoryRequest request)
    {
        try
        {
            var response = _service.CreateJobHistory(request);
            return CreatedAtAction(nameof(GetJobHistoryById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("external")]
    public ActionResult CreateExternalJobHistory(CreateExternalJobHistoryRequest request)
    {
        try
        {
            var response = _service.CreateExternalJobHistory(request);
            return CreatedAtAction(nameof(GetJobHistoryById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}
