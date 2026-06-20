using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Telemetry.Dtos;
using RobotControllerApi.BoundedContexts.Telemetry.Services;

namespace RobotControllerApi.BoundedContexts.Telemetry.Controllers;

[ApiController]
[Route("api")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _service;

    public TelemetryController(ITelemetryService service)
    {
        _service = service;
    }

    [Authorize(Policy = AuthorizationPolicies.DeviceAdapter)]
    [HttpPost("adapter/devices/{deviceId:int}/telemetry")]
    public ActionResult CreateTelemetryReading(int deviceId, CreateTelemetryReadingRequest request)
    {
        try
        {
            var response = _service.CreateTelemetryReading(deviceId, request);
            return CreatedAtAction(nameof(GetLatestTelemetryReadingByDeviceId), new { deviceId }, response);
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

    [Authorize(Policy = AuthorizationPolicies.HumanUser)]
    [HttpGet("devices/{deviceId:int}/telemetry")]
    public ActionResult GetTelemetryReadingsByDeviceId(
        int deviceId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int limit = 100)
    {
        try
        {
            return Ok(_service.GetTelemetryReadingsByDeviceId(deviceId, fromUtc, toUtc, limit));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.HumanUser)]
    [HttpGet("devices/{deviceId:int}/telemetry/latest")]
    public ActionResult GetLatestTelemetryReadingByDeviceId(int deviceId)
    {
        try
        {
            var response = _service.GetLatestTelemetryReadingByDeviceId(deviceId);
            if (response == null) return NotFound();
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.HumanUser)]
    [HttpGet("devices/{deviceId:int}/telemetry/summary")]
    public ActionResult GetLatestTelemetrySummaryByDeviceId(int deviceId)
    {
        try
        {
            return Ok(_service.GetLatestTelemetrySummaryByDeviceId(deviceId));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
