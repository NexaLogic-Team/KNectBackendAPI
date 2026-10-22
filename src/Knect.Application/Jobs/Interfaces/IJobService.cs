using Knect.Application.Jobs.DTOs;
using Knect.Domain.Enums;

namespace Knect.Application.Jobs.Interfaces;

public interface IJobService
{
    Task<JobSummaryDto> CreateAsync(Guid employerUserId, CreateJobRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<JobSummaryDto>> GetEmployerJobsAsync(Guid employerUserId, CancellationToken cancellationToken);
    Task<JobDetailDto> GetEmployerJobAsync(Guid employerUserId, Guid jobId, CancellationToken cancellationToken);
    Task<JobSummaryDto> UpdateAsync(Guid employerUserId, Guid jobId, UpdateJobRequest request, CancellationToken cancellationToken);
    Task<JobSummaryDto> UpdateStatusAsync(Guid employerUserId, Guid jobId, JobStatusUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteDraftAsync(Guid employerUserId, Guid jobId, CancellationToken cancellationToken);
    Task<PagedResult<JobSummaryDto>> SearchPublicAsync(JobSearchRequest request, CancellationToken cancellationToken);
    Task<JobDetailDto> GetPublicAsync(Guid jobId, CancellationToken cancellationToken);
}
