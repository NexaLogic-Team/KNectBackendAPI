using Knect.Application.Authentication.Exceptions;
using Knect.Application.Subscriptions.DTOs;
using Knect.Application.Subscriptions.Interfaces;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Subscriptions;

public sealed class EmployerSubscriptionService : IEmployerSubscriptionService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly KnectDbContext _dbContext;

    public EmployerSubscriptionService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetAvailablePlansAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Price)
            .Select(x => new SubscriptionPlanDto(
                x.Id,
                x.Name,
                x.Description,
                x.Price,
                x.Currency,
                x.DurationDays,
                x.MaxJobPosts,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionSummaryDto>> GetMySubscriptionsAsync(Guid employerUserId, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);

        return await _dbContext.Subscriptions
            .AsNoTracking()
            .Where(x => x.EmployerProfileId == employerProfileId)
            .Include(x => x.Plan)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SubscriptionSummaryDto(
                x.Id,
                x.PlanId,
                x.Plan.Name,
                x.Plan.Price,
                x.Plan.Currency,
                x.Plan.DurationDays,
                x.Plan.MaxJobPosts,
                x.Status,
                x.StartDate,
                x.EndDate,
                x.IsAutoRenew,
                x.Notes,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionSummaryDto> PurchasePlanAsync(Guid employerUserId, EmployerSubscriptionPurchaseRequest request, CancellationToken cancellationToken)
    {
        if (request.PlanId == Guid.Empty)
        {
            throw new AuthException("Plan is required.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            throw new AuthException("Payment method is required.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.PaymentReference))
        {
            throw new AuthException("Payment reference is required.", 400);
        }

        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive, cancellationToken)
            ?? throw new AuthException("Selected plan is not available.", 404);

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            EmployerProfileId = employerProfileId,
            PlanId = plan.Id,
            UserId = employerUserId,
            StartDate = now,
            EndDate = now.AddDays(plan.DurationDays),
            Status = SubscriptionStatus.Pending,
            IsAutoRenew = request.AutoRenew,
            Notes = request.Notes?.Trim(),
            CreatedAt = now
        };

        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var payment = new PaymentRecord
        {
            EmployerProfileId = employerProfileId,
            SubscriptionId = subscription.Id,
            Amount = plan.Price,
            Currency = plan.Currency,
            PaymentMethod = request.PaymentMethod.Trim(),
            PaymentReference = request.PaymentReference.Trim(),
            EvidenceFileKey = request.EvidenceFileKey,
            SubmittedAt = now,
            Status = PaymentStatus.Pending,
            ReviewNotes = null,
            CreatedAt = now
        };

        _dbContext.PaymentRecords.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubscriptionSummaryDto(
            subscription.Id,
            subscription.PlanId,
            plan.Name,
            plan.Price,
            plan.Currency,
            plan.DurationDays,
            plan.MaxJobPosts,
            subscription.Status,
            subscription.StartDate,
            subscription.EndDate,
            subscription.IsAutoRenew,
            subscription.Notes,
            subscription.CreatedAt);
    }

    public async Task<IReadOnlyList<PaymentRecordDto>> GetMyPaymentsAsync(Guid employerUserId, CancellationToken cancellationToken)
    {
        var employerProfileId = await GetEmployerProfileIdAsync(employerUserId, cancellationToken);

        return await _dbContext.PaymentRecords
            .AsNoTracking()
            .Where(x => x.EmployerProfileId == employerProfileId)
            .Include(x => x.ReviewedByAdmin)
            .OrderByDescending(x => x.SubmittedAt)
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
    }

    private async Task<Guid> GetEmployerProfileIdAsync(Guid employerUserId, CancellationToken cancellationToken)
    {
        var profileId = await _dbContext.EmployerProfiles
            .Where(x => x.UserId == employerUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (profileId == Guid.Empty)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        return profileId;
    }
}
