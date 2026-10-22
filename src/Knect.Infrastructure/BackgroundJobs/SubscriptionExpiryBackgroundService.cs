using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Knect.Infrastructure.BackgroundJobs;

public sealed class SubscriptionExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<SubscriptionLifecycleOptions> _options;
    private readonly ILogger<SubscriptionExpiryBackgroundService> _logger;

    public SubscriptionExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SubscriptionLifecycleOptions> options,
        ILogger<SubscriptionExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Subscription lifecycle worker is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _options.Value.CheckIntervalMinutes)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleService>();
                var expired = await service.ProcessExpiredSubscriptionsAsync(stoppingToken);
                var reminders = await service.ProcessReminderNotificationsAsync(stoppingToken);
                _logger.LogInformation("Subscription lifecycle worker completed: expired {ExpiredCount}, reminders {ReminderCount}.", expired, reminders);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription lifecycle worker failed and will retry on the next interval.");
            }
        }
    }
}
