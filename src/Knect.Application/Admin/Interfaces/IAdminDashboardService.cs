using Knect.Application.Admin.DTOs;

namespace Knect.Application.Admin.Interfaces;

public interface IAdminDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
    Task<PagedResult<RecentRegistrationDto>> GetRecentRegistrationsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResult<RecentJobDto>> GetRecentJobsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResult<RecentPaymentReviewDto>> GetRecentPaymentsAsync(int page, int pageSize, CancellationToken cancellationToken);
}
