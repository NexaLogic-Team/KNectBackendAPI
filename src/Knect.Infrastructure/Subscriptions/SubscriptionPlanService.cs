using Knect.Application.Authentication.Exceptions;
using Knect.Application.Subscriptions.DTOs;
using Knect.Application.Subscriptions.Interfaces;
using Knect.Domain.Entities;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Subscriptions;

public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly KnectDbContext _dbContext;

    public SubscriptionPlanService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = _dbContext.SubscriptionPlans.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
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

    public async Task<SubscriptionPlanDto> GetPlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == planId, cancellationToken)
            ?? throw new AuthException("Subscription plan not found.", 404);

        return Map(plan);
    }

    public async Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        var name = request.Name.Trim();
        if (await _dbContext.SubscriptionPlans.AnyAsync(x => x.Name == name, cancellationToken))
        {
            throw new AuthException("A plan with this name already exists.", 409);
        }

        var plan = new SubscriptionPlan
        {
            Name = name,
            Description = request.Description?.Trim(),
            Price = request.Price,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            DurationDays = request.DurationDays,
            MaxJobPosts = request.MaxJobPosts,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.SubscriptionPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(plan);
    }

    public async Task<SubscriptionPlanDto> UpdateAsync(Guid planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId, cancellationToken)
            ?? throw new AuthException("Subscription plan not found.", 404);

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim() != plan.Name)
        {
            var normalizedName = request.Name.Trim();
            var duplicate = await _dbContext.SubscriptionPlans.AnyAsync(x => x.Id != planId && x.Name == normalizedName, cancellationToken);
            if (duplicate)
            {
                throw new AuthException("A plan with this name already exists.", 409);
            }

            plan.Name = normalizedName;
        }

        if (request.Description is not null) { plan.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(); }
        if (request.Price.HasValue) { plan.Price = request.Price.Value; }
        if (request.Currency is not null) { plan.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(); }
        if (request.DurationDays.HasValue) { plan.DurationDays = request.DurationDays.Value; }
        if (request.MaxJobPosts.HasValue) { plan.MaxJobPosts = request.MaxJobPosts.Value; }
        if (request.IsActive.HasValue) { plan.IsActive = request.IsActive.Value; }

        ValidatePlan(plan);
        plan.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(plan);
    }

    public async Task<SubscriptionPlanDto> SetActiveAsync(Guid planId, bool isActive, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId, cancellationToken)
            ?? throw new AuthException("Subscription plan not found.", 404);

        plan.IsActive = isActive;
        plan.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(plan);
    }

    private static void ValidateCreateRequest(CreateSubscriptionPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new AuthException("Plan name is required.", 400);
        if (request.Name.Trim().Length > 120) throw new AuthException("Plan name is too long.", 400);
        if (request.Price < 0m) throw new AuthException("Price cannot be negative.", 400);
        if (request.DurationDays <= 0) throw new AuthException("Duration days must be greater than zero.", 400);
        if (request.MaxJobPosts < 0) throw new AuthException("Maximum job posts cannot be negative.", 400);
        if (string.IsNullOrWhiteSpace(request.Currency)) throw new AuthException("Currency is required.", 400);
        ValidatePlan(new SubscriptionPlan
        {
            Name = request.Name,
            Price = request.Price,
            Currency = request.Currency,
            DurationDays = request.DurationDays,
            MaxJobPosts = request.MaxJobPosts,
            IsActive = request.IsActive
        });
    }

    private static void ValidatePlan(SubscriptionPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.Name)) throw new AuthException("Plan name is required.", 400);
        if (plan.Price < 0m) throw new AuthException("Price cannot be negative.", 400);
        if (string.IsNullOrWhiteSpace(plan.Currency)) throw new AuthException("Currency is required.", 400);
        if (plan.DurationDays <= 0) throw new AuthException("Duration days must be greater than zero.", 400);
        if (plan.MaxJobPosts < 0) throw new AuthException("Maximum job posts cannot be negative.", 400);
    }

    private static SubscriptionPlanDto Map(SubscriptionPlan plan)
        => new(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Price,
            plan.Currency,
            plan.DurationDays,
            plan.MaxJobPosts,
            plan.IsActive,
            plan.CreatedAt,
            plan.UpdatedAt);
}
