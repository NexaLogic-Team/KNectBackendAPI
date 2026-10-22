using Knect.Application.Subscriptions.DTOs;

namespace Knect.Application.Subscriptions.Interfaces;

public interface ISubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<SubscriptionPlanDto> GetPlanAsync(Guid planId, CancellationToken cancellationToken);
    Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken);
    Task<SubscriptionPlanDto> UpdateAsync(Guid planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken);
    Task<SubscriptionPlanDto> SetActiveAsync(Guid planId, bool isActive, CancellationToken cancellationToken);
}
