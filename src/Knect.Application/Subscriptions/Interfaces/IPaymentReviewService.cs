using Knect.Application.Subscriptions.DTOs;

namespace Knect.Application.Subscriptions.Interfaces;

public interface IPaymentReviewService
{
    Task<PagedResult<PaymentRecordDto>> GetPendingPaymentsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PaymentRecordDto> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentRecordDto> ReviewPaymentAsync(Guid adminUserId, Guid paymentId, ReviewPaymentRequest request, CancellationToken cancellationToken);
}
