using Knect.Application.Applications.DTOs;
using Knect.Application.Applications.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Knect.Application.FileStorage;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Applications;

public sealed class ApplicationService : IApplicationService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly KnectDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;

    public ApplicationService(KnectDbContext dbContext, IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
    }

    public async Task<ApplicationSummaryDto> ApplyAsync(Guid applicantUserId, Guid jobId, ApplyToJobRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CoverLetter) && string.IsNullOrWhiteSpace(request.CvFileKey))
        {
            throw new AuthException("Either a cover letter or a CV reference is required.", 400);
        }

        if (!string.IsNullOrWhiteSpace(request.CoverLetter) && request.CoverLetter.Length > 5000)
        {
            throw new AuthException("Cover letter is too long.", 400);
        }

        var job = await _dbContext.Jobs
            .Include(x => x.EmployerProfile)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == jobId && !x.IsDeleted, cancellationToken)
            ?? throw new AuthException("Job not found.", 404);

        if (job.Status != JobStatus.Published)
        {
            throw new AuthException("Applications are only allowed for published jobs.", 400);
        }

        if (job.ExpiresAt.HasValue && job.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AuthException("This job is no longer accepting applications.", 400);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == applicantUserId && !x.IsDeleted, cancellationToken)
            ?? throw new AuthException("User not found.", 404);

        if (!string.IsNullOrWhiteSpace(request.CvFileKey))
        {
            if (request.CvFileKey.Contains("..", StringComparison.Ordinal) || request.CvFileKey.Contains("/", StringComparison.Ordinal) || request.CvFileKey.Contains("\\", StringComparison.Ordinal))
            {
                throw new AuthException("Invalid CV storage key.", 400);
            }

            if (string.IsNullOrWhiteSpace(user.CvFileKey) || !string.Equals(user.CvFileKey, request.CvFileKey, StringComparison.Ordinal))
            {
                throw new AuthException("CV reference does not belong to the authenticated user.", 403);
            }
        }

        var existing = await _dbContext.JobApplications
            .AsTracking()
            .FirstOrDefaultAsync(x => x.JobId == jobId && x.UserId == applicantUserId && x.Status != ApplicationStatus.Cancelled, cancellationToken);

        if (existing is not null)
        {
            throw new AuthException("You have already applied to this job.", 409);
        }

        var application = new JobApplication
        {
            JobId = jobId,
            UserId = applicantUserId,
            CoverLetter = request.CoverLetter?.Trim(),
            CvFileKey = request.CvFileKey?.Trim(),
            Status = ApplicationStatus.Submitted,
            AppliedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.JobApplications.Add(application);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ApplicationSummaryDto(
            application.Id,
            application.JobId,
            job.Title,
            job.EmployerProfile.CompanyName,
            application.Status,
            application.AppliedAt,
            application.UpdatedAt,
            false);
    }

    public async Task<PagedResult<ApplicationSummaryDto>> GetMyApplicationsAsync(Guid applicantUserId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _dbContext.JobApplications
            .AsNoTracking()
            .Where(x => x.UserId == applicantUserId)
            .Include(x => x.Job)
            .ThenInclude(x => x.EmployerProfile)
            .AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.AppliedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new ApplicationSummaryDto(
                x.Id,
                x.JobId,
                x.Job.Title,
                x.Job.EmployerProfile.CompanyName,
                x.Status,
                x.AppliedAt,
                x.UpdatedAt,
                x.Status == ApplicationStatus.Cancelled))
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationSummaryDto>(items, safePage, safePageSize, totalCount);
    }

    public async Task<ApplicationDetailDto> GetMyApplicationAsync(Guid applicantUserId, Guid applicationId, CancellationToken cancellationToken)
    {
        var app = await _dbContext.JobApplications
            .Include(x => x.Job)
            .ThenInclude(x => x.EmployerProfile)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.UserId == applicantUserId, cancellationToken)
            ?? throw new AuthException("Application not found.", 404);

        return new ApplicationDetailDto(
            app.Id,
            app.JobId,
            app.Job.Title,
            app.Job.EmployerProfile.CompanyName,
            app.Status,
            app.CoverLetter,
            app.CvFileKey,
            app.AppliedAt,
            app.UpdatedAt,
            app.Status == ApplicationStatus.Cancelled,
            app.User.FullName,
            app.User.Email,
            true);
    }

    public async Task<ApplicationSummaryDto> WithdrawAsync(Guid applicantUserId, Guid applicationId, CancellationToken cancellationToken)
    {
        var app = await _dbContext.JobApplications
            .Include(x => x.Job)
            .ThenInclude(x => x.EmployerProfile)
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.UserId == applicantUserId, cancellationToken)
            ?? throw new AuthException("Application not found.", 404);

        if (app.Status == ApplicationStatus.Cancelled)
        {
            throw new AuthException("Application is already withdrawn.", 400);
        }

        if (app.Status == ApplicationStatus.Accepted || app.Status == ApplicationStatus.Rejected)
        {
            throw new AuthException("This application can no longer be withdrawn.", 400);
        }

        app.Status = ApplicationStatus.Cancelled;
        app.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ApplicationSummaryDto(
            app.Id,
            app.JobId,
            app.Job.Title,
            app.Job.EmployerProfile.CompanyName,
            app.Status,
            app.AppliedAt,
            app.UpdatedAt,
            true);
    }

    public async Task<PagedResult<ApplicationSummaryDto>> GetEmployerApplicationsAsync(Guid employerUserId, EmployerApplicationFilter filter, CancellationToken cancellationToken)
    {
        var employerProfileId = await _dbContext.EmployerProfiles
            .Where(x => x.UserId == employerUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfileId == Guid.Empty)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        var safePage = filter.Page <= 0 ? 1 : filter.Page;
        var safePageSize = filter.PageSize <= 0 ? DefaultPageSize : Math.Min(filter.PageSize, MaxPageSize);

        var query = _dbContext.JobApplications
            .AsNoTracking()
            .Include(x => x.Job)
            .ThenInclude(x => x.EmployerProfile)
            .Where(x => x.Job.EmployerProfileId == employerProfileId);

        if (filter.JobId.HasValue)
        {
            query = query.Where(x => x.JobId == filter.JobId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.AppliedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new ApplicationSummaryDto(
                x.Id,
                x.JobId,
                x.Job.Title,
                x.Job.EmployerProfile.CompanyName,
                x.Status,
                x.AppliedAt,
                x.UpdatedAt,
                x.Status == ApplicationStatus.Cancelled))
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationSummaryDto>(items, safePage, safePageSize, totalCount);
    }

    public async Task<ApplicationDetailDto> GetEmployerApplicationAsync(Guid employerUserId, Guid applicationId, CancellationToken cancellationToken)
    {
        var employerProfileId = await _dbContext.EmployerProfiles
            .Where(x => x.UserId == employerUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfileId == Guid.Empty)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        var app = await _dbContext.JobApplications
            .Include(x => x.Job)
            .ThenInclude(x => x.EmployerProfile)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.Job.EmployerProfileId == employerProfileId, cancellationToken)
            ?? throw new AuthException("Application not found.", 404);

        return new ApplicationDetailDto(
            app.Id,
            app.JobId,
            app.Job.Title,
            app.Job.EmployerProfile.CompanyName,
            app.Status,
            app.CoverLetter,
            app.CvFileKey,
            app.AppliedAt,
            app.UpdatedAt,
            app.Status == ApplicationStatus.Cancelled,
            app.User.FullName,
            app.User.Email,
            true);
    }

    public async Task<ApplicationSummaryDto> UpdateStatusAsync(Guid employerUserId, Guid applicationId, UpdateApplicationStatusRequest request, CancellationToken cancellationToken)
    {
        var employerProfileId = await _dbContext.EmployerProfiles
            .Where(x => x.UserId == employerUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfileId == Guid.Empty)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        var app = await _dbContext.JobApplications
            .Include(x => x.Job)
            .ThenInclude(x => x.EmployerProfile)
            .FirstOrDefaultAsync(x => x.Id == applicationId && x.Job.EmployerProfileId == employerProfileId, cancellationToken)
            ?? throw new AuthException("Application not found.", 404);

        if (!JobApplication.IsTransitionAllowed(app.Status, request.Status))
        {
            throw new AuthException($"Invalid application transition from {app.Status} to {request.Status}.", 400);
        }

        app.Status = request.Status;
        app.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ApplicationSummaryDto(
            app.Id,
            app.JobId,
            app.Job.Title,
            app.Job.EmployerProfile.CompanyName,
            app.Status,
            app.AppliedAt,
            app.UpdatedAt,
            app.Status == ApplicationStatus.Cancelled);
    }
}
