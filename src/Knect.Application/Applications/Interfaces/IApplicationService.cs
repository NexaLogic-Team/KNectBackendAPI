using Knect.Application.Applications.DTOs;
using Knect.Domain.Enums;

namespace Knect.Application.Applications.Interfaces;

public interface IApplicationService
{
    Task<ApplicationSummaryDto> ApplyAsync(Guid applicantUserId, Guid jobId, ApplyToJobRequest request, CancellationToken cancellationToken);
    Task<PagedResult<ApplicationSummaryDto>> GetMyApplicationsAsync(Guid applicantUserId, int page, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationDetailDto> GetMyApplicationAsync(Guid applicantUserId, Guid applicationId, CancellationToken cancellationToken);
    Task<ApplicationSummaryDto> WithdrawAsync(Guid applicantUserId, Guid applicationId, CancellationToken cancellationToken);
    Task<PagedResult<ApplicationSummaryDto>> GetEmployerApplicationsAsync(Guid employerUserId, EmployerApplicationFilter filter, CancellationToken cancellationToken);
    Task<ApplicationDetailDto> GetEmployerApplicationAsync(Guid employerUserId, Guid applicationId, CancellationToken cancellationToken);
    Task<ApplicationSummaryDto> UpdateStatusAsync(Guid employerUserId, Guid applicationId, UpdateApplicationStatusRequest request, CancellationToken cancellationToken);
}
