using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Notifications;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Knect.Infrastructure.BackgroundJobs;

public sealed class SubscriptionLifecycleService
{
    private readonly KnectDbContext _dbContext;
    private readonly PlatformNotificationService _notificationService;
    private readonly IOptions<SubscriptionLifecycleOptions> _options;
    private readonly ILogger<SubscriptionLifecycleService> _logger;

    public SubscriptionLifecycleService(
        KnectDbContext dbContext,
        PlatformNotificationService notificationService,
        IOptions<SubscriptionLifecycleOptions> options,
        ILogger<SubscriptionLifecycleService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _options = options;
        _logger = logger;
    }

    public async Task<int> ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var subscriptions = await _dbContext.Subscriptions
            .Include(x => x.EmployerProfile)
            .Where(x => x.Status == SubscriptionStatus.Active && x.EndDate <= now)
            .OrderBy(x => x.EndDate)
            .Take(_options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var subscription in subscriptions)
        {
            try
            {
                if (subscription.Status != SubscriptionStatus.Active)
                {
                    continue;
                }

                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedAt = now;
                subscription.Notes = string.IsNullOrWhiteSpace(subscription.Notes) ? "Subscription expired automatically." : subscription.Notes;

                var jobsToClose = await _dbContext.Jobs
                    .Where(x => x.EmployerProfileId == subscription.EmployerProfileId && !x.IsDeleted && (x.Status == JobStatus.Published || x.Status == JobStatus.Paused))
                    .ToListAsync(cancellationToken);

                foreach (var job in jobsToClose)
                {
                    job.Status = JobStatus.Closed;
                    job.UpdatedAt = now;
                }

                processed++;
                _logger.LogInformation("Expired subscription {SubscriptionId} for employer {EmployerProfileId}.", subscription.Id, subscription.EmployerProfileId);
                await _notificationService.NotifySubscriptionReminderAsync(subscription, "expired", subscription.EndDate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire subscription {SubscriptionId}.", subscription.Id);
            }
        }

        if (processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }

    public async Task<int> ProcessReminderNotificationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var processed = 0;

        foreach (var reminderDays in new[] { _options.Value.ExpiryReminderDays, _options.Value.FinalReminderDays }.Distinct())
        {
            var cutoff = now.AddDays(reminderDays);
            var subscriptions = await _dbContext.Subscriptions
                .Include(x => x.User)
                .Where(x => x.Status == SubscriptionStatus.Active && x.EndDate > now && x.EndDate <= cutoff)
                .OrderBy(x => x.EndDate)
                .Take(_options.Value.BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var subscription in subscriptions)
            {
                if (subscription.User is null)
                {
                    continue;
                }

                var reminderType = reminderDays == _options.Value.FinalReminderDays ? "expiring-soon-final" : "expiring-soon";
                var dedupeMarker = $"{reminderType}:{subscription.Id}";
                var alreadySent = await _dbContext.AuditLogs.AnyAsync(x => x.EventType == "Notification" && x.Message == dedupeMarker, cancellationToken);
                if (alreadySent)
                {
                    continue;
                }

                try
                {
                    await _notificationService.NotifySubscriptionReminderAsync(subscription, reminderType, subscription.EndDate, cancellationToken);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder for subscription {SubscriptionId}.", subscription.Id);
                }
            }
        }

        return processed;
    }

    public async Task<int> ProcessSubscriptionRemindersAsync(CancellationToken cancellationToken)
    {
        return await ProcessReminderNotificationsAsync(cancellationToken);
    }
}
