using Knect.Application.Admin.DTOs;

namespace Knect.Application.Admin.Interfaces;

public interface IAdminReportService
{
    Task<IReadOnlyList<AdminReportRowDto>> GetUsersReportAsync(ReportFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminReportRowDto>> GetJobsReportAsync(ReportFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminReportRowDto>> GetApplicationsReportAsync(ReportFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminReportRowDto>> GetSubscriptionsReportAsync(ReportFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminReportRowDto>> GetPaymentsReportAsync(ReportFilter filter, CancellationToken cancellationToken);
}
