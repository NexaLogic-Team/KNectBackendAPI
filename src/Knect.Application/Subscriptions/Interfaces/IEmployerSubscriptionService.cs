using Knect.Application.Subscriptions.DTOs;

namespace Knect.Application.Subscriptions.Interfaces;

public interface IEmployerSubscriptionService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetAvailablePlansAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SubscriptionSummaryDto>> GetMySubscriptionsAsync(Guid employerUserId, CancellationToken cancellationToken);
    Task<SubscriptionSummaryDto> PurchasePlanAsync(Guid employerUserId, EmployerSubscriptionPurchaseRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentRecordDto>> GetMyPaymentsAsync(Guid employerUserId, CancellationToken cancellationToken);
}
