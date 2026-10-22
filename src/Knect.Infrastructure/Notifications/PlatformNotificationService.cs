using Knect.Application.Authentication.Interfaces;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Knect.Infrastructure.Notifications;

public sealed class PlatformNotificationService
{
    private readonly KnectDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PlatformNotificationService> _logger;

    public PlatformNotificationService(KnectDbContext dbContext, IEmailSender emailSender, ILogger<PlatformNotificationService> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task NotifyPaymentDecisionAsync(PaymentRecord payment, PaymentStatus newStatus, string? reason, CancellationToken cancellationToken)
    {
        var employer = await _dbContext.EmployerProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == payment.EmployerProfileId, cancellationToken);

        if (employer?.User is null)
        {
            return;
        }

        var email = employer.User.Email;
        var decision = newStatus == PaymentStatus.Confirmed ? "confirmed" : "rejected";
        var body = $"Your payment for {payment.Currency} {payment.Amount:0.00} was {decision}." + (string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason}");

        await RecordNotificationAsync(payment.Id, $"Payment{decision}", body, email, cancellationToken);
        await _emailSender.SendEmailAsync(email, $"Payment {decision}", body, cancellationToken);
    }

    public async Task NotifyApplicationSubmittedAsync(JobApplication application, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .Include(x => x.EmployerProfile)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == application.JobId, cancellationToken);

        if (job?.EmployerProfile?.User is null)
        {
            return;
        }

        var body = $"A new application has been submitted for {job.Title}.";
        await RecordNotificationAsync(application.Id, "ApplicationSubmitted", body, job.EmployerProfile.User.Email, cancellationToken);
        await _emailSender.SendEmailAsync(job.EmployerProfile.User.Email, "New application", body, cancellationToken);
    }

    public async Task NotifyApplicationStatusChangedAsync(JobApplication application, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == application.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var body = $"Your application status has been updated to {application.Status}.";
        await RecordNotificationAsync(application.Id, $"Application{application.Status}", body, user.Email, cancellationToken);
        await _emailSender.SendEmailAsync(user.Email, "Application update", body, cancellationToken);
    }

    public async Task NotifySubscriptionReminderAsync(Subscription subscription, string reminderType, DateTime expiresAt, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == subscription.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var subject = reminderType switch
        {
            "expiring-soon" => "Subscription expiring soon",
            "expiring-soon-final" => "Subscription expiring soon",
            "expired" => "Subscription expired",
            _ => "Subscription update"
        };

        var body = $"Your subscription expires on {expiresAt:yyyy-MM-dd HH:mm 'UTC'}.";
        await RecordNotificationAsync(subscription.Id, reminderType, body, user.Email, cancellationToken);
        await _emailSender.SendEmailAsync(user.Email, subject, body, cancellationToken);
    }

    private async Task RecordNotificationAsync(Guid entityId, string notificationType, string body, string email, CancellationToken cancellationToken)
    {
        var dedupeKey = $"{notificationType}:{entityId}";
        var existing = await _dbContext.AuditLogs
            .AsNoTracking()
            .AnyAsync(x => x.EventType == "Notification" && x.Message == dedupeKey, cancellationToken);

        if (existing)
        {
            _logger.LogInformation("Skipping duplicate notification {NotificationType} for entity {EntityId}.", notificationType, entityId);
            return;
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            EventType = "Notification",
            Message = dedupeKey,
            DetailsJson = $"{{\"channel\":\"email\",\"recipient\":\"{email}\",\"body\":\"{body.Replace("\"", "\\\"")}\"}}",
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
