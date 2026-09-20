using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RobotControllerApi.Infrastructure.DataAccess;

namespace RobotControllerApi.Infrastructure.Health;

// A liveness/readiness probe for the container runtime and the deployment pipeline.
//
// [AllowAnonymous] is required, not incidental: the authorization fallback policy denies any
// unattributed endpoint, so without it Docker's HEALTHCHECK and any deploy-time smoke test
// would be answered with 401 and the container would never be reported healthy.
[ApiController]
[AllowAnonymous]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly DbConfig _dbConfig;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DbConfig dbConfig, ILogger<HealthController> logger)
    {
        _dbConfig = dbConfig;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        // Actually open a connection and make the server answer. A probe that only reports
        // that the process is up would keep a container marked healthy while every request
        // touching the database fails, which is exactly the state a deploy needs to catch.
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_dbConfig.GetConnectionString())
            {
                // Keep the probe shorter than the HEALTHCHECK timeout, so a hung database
                // surfaces as an unhealthy container rather than a hung probe.
                Timeout = 3,
                CommandTimeout = 3
            };

            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return Ok(new { status = "healthy", database = "connected" });
        }
        catch (Exception ex)
        {
            // The detail goes to the log, not the response: the connection string's host and
            // username can appear in Npgsql's messages, and this endpoint is unauthenticated.
            _logger.LogError(ex, "Health check failed: database unreachable.");

            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unhealthy", database = "unreachable" });
        }
    }
}
