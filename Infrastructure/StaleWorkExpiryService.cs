using RobotControllerApi.BoundedContexts.Jobs.Services;

namespace RobotControllerApi.Infrastructure;

// Drains expired leases on a timer.
//
// Without this, expiry only happened inside claim-next, which meant the queue could only be
// unblocked by the very robot that had stopped responding. A robot that browns out or drops
// off mid-run would leave work Executing forever, and the per-device queue cap would then
// reject every new job until someone intervened by hand.
public class StaleWorkExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StaleWorkExpiryService> _logger;
    private readonly TimeSpan _interval;

    public StaleWorkExpiryService(IServiceProvider serviceProvider, ILogger<StaleWorkExpiryService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var configuredSeconds = int.TryParse(configuration["WorkDispatch:StaleExpirySweepSeconds"], out var parsed) && parsed > 0 ? parsed : 30;
        _interval = TimeSpan.FromSeconds(configuredSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                // The dispatch service is scoped, so the sweep needs its own scope per tick.
                using var scope = _serviceProvider.CreateScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<IWorkDispatchService>();

                var expired = dispatchService.ExpireStaleWorkForAllDevices();
                if (expired > 0)
                {
                    _logger.LogInformation("Stale work sweep settled {ExpiredCount} expired job lease(s).", expired);
                }
            }
            catch (Exception ex)
            {
                // A failed sweep must never take the host down; the next tick tries again.
                _logger.LogError(ex, "Stale work sweep failed.");
            }
        }
    }
}
