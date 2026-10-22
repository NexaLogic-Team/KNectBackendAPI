using Knect.Application.Authentication.Exceptions;
using Knect.Application.Subscriptions.DTOs;
using Knect.Application.Subscriptions.Interfaces;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Subscriptions;

public sealed class PaymentReviewService : IPaymentReviewService
{
    private readonly KnectDbContext _dbContext;

    public PaymentReviewService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PaymentRecordDto>> GetPendingPaymentsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.PaymentRecords
            .AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending)
            .Include(x => x.ReviewedByAdmin)
            .OrderByDescending(x => x.SubmittedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new PaymentRecordDto(
                x.Id,
                x.EmployerProfileId,
                x.SubscriptionId,
                x.Amount,
                x.Currency,
                x.PaymentMethod,
                x.PaymentReference,
                x.EvidenceFileKey,
                x.Status,
                x.SubmittedAt,
                x.ReviewedAt,
                x.ReviewNotes,
                x.ReviewedByAdminId,
                x.ReviewedByAdmin != null ? x.ReviewedByAdmin.FirstName + " " + x.ReviewedByAdmin.LastName : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentRecordDto>(items, safePage, safePageSize, totalCount);
    }

    public async Task<PaymentRecordDto> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.PaymentRecords
            .AsNoTracking()
            .Include(x => x.ReviewedByAdmin)
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
            ?? throw new AuthException("Payment record not found.", 404);

        return Map(payment);
    }

    public async Task<PaymentRecordDto> ReviewPaymentAsync(Guid adminUserId, Guid paymentId, ReviewPaymentRequest request, CancellationToken cancellationToken)
    {
        if (request.Status != PaymentStatus.Confirmed && request.Status != PaymentStatus.Rejected)
        {
            throw new AuthException("Payment status must be either Confirmed or Rejected.", 400);
        }

        var payment = await _dbContext.PaymentRecords
            .Include(x => x.Subscription)
            .ThenInclude(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
            ?? throw new AuthException("Payment record not found.", 404);

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new AuthException("Only pending payments can be reviewed.", 400);
        }

        var adminUser = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken)
            ?? throw new AuthException("Admin user not found.", 404);

        var hasAdminRole = adminUser.UserRoles.Any(x => x.Role.Name == "Admin");
        if (!hasAdminRole)
        {
            throw new AuthException("Admin privileges are required.", 403);
        }

        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        payment.Status = request.Status;
        payment.ReviewNotes = request.ReviewNotes?.Trim();
        payment.ReviewedAt = DateTime.UtcNow;
        payment.ReviewedByAdminId = adminUserId;

        if (request.Status == PaymentStatus.Confirmed)
        {
            if (payment.SubscriptionId is null)
            {
                throw new AuthException("The payment is not linked to a subscription.", 400);
            }

            var subscription = payment.Subscription ?? await _dbContext.Subscriptions
                .Include(x => x.Plan)
                .FirstOrDefaultAsync(x => x.Id == payment.SubscriptionId.Value, cancellationToken)
                ?? throw new AuthException("Subscription not found.", 404);

            var plan = subscription.Plan ?? await _dbContext.SubscriptionPlans.FirstAsync(x => x.Id == subscription.PlanId, cancellationToken);

            subscription.Status = SubscriptionStatus.Active;
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow.AddDays(plan.DurationDays);
            subscription.Notes = string.IsNullOrWhiteSpace(subscription.Notes) ? "Activated after manual verification." : subscription.Notes;
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        if (request.Status == PaymentStatus.Rejected)
        {
            if (payment.SubscriptionId is Guid subscriptionId)
            {
                var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);
                if (subscription is not null)
                {
                    subscription.Status = SubscriptionStatus.Cancelled;
                    subscription.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Map(payment);
    }

    private static PaymentRecordDto Map(PaymentRecord payment)
        => new(
            payment.Id,
            payment.EmployerProfileId,
            payment.SubscriptionId,
            payment.Amount,
            payment.Currency,
            payment.PaymentMethod,
            payment.PaymentReference,
            payment.EvidenceFileKey,
            payment.Status,
            payment.SubmittedAt,
            payment.ReviewedAt,
            payment.ReviewNotes,
            payment.ReviewedByAdminId,
            payment.ReviewedByAdmin != null ? payment.ReviewedByAdmin.FirstName + " " + payment.ReviewedByAdmin.LastName : null);
}
