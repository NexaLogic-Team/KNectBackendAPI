using Knect.Application.Authentication.Exceptions;
using Knect.Application.Jobs.DTOs;
using Knect.Application.Jobs.Interfaces;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.BackgroundJobs;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Jobs;

public sealed class JobService : IJobService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly KnectDbContext _dbContext;

    public JobService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JobSummaryDto> CreateAsync(Guid employerUserId, CreateJobRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var employerProfile = await _dbContext.EmployerProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == employerUserId, cancellationToken)
            ?? throw new AuthException("Employer profile not found.", 404);

        var category = await _dbContext.JobCategories.FirstOrDefaultAsync(x => x.Id == request.CategoryId && x.IsActive, cancellationToken)
            ?? throw new AuthException("Selected category is not available.", 400);

        var experience = await _dbContext.JobExperiences.FirstOrDefaultAsync(x => x.Id == request.ExperienceId && x.IsActive, cancellationToken)
            ?? throw new AuthException("Selected experience level is not available.", 400);

        var activeSubscription = await _dbContext.Subscriptions
            .Include(x => x.Plan)
            .Where(x => x.EmployerProfileId == employerProfile.Id && x.Status == SubscriptionStatus.Active && x.EndDate > DateTime.UtcNow)
            .OrderByDescending(x => x.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.Status == JobStatus.Published || request.Status == JobStatus.Paused)
        {
            if (activeSubscription is null)
            {
                throw new AuthException("This employer does not have an active subscription that allows publishing jobs.", 403);
            }

            var activePublishedCount = await _dbContext.Jobs.CountAsync(x => x.EmployerProfileId == employerProfile.Id && x.Status == JobStatus.Published && !x.IsDeleted, cancellationToken);
            if (!SubscriptionPublishingPolicy.IsEligibleForPublishing(activeSubscription, activePublishedCount, activeSubscription.Plan.MaxJobPosts, DateTime.UtcNow))
            {
                throw new AuthException("Job publication limit reached for the active subscription.", 403);
            }
        }

        var entity = new Job
        {
            EmployerProfileId = employerProfile.Id,
            CategoryId = category.Id,
            ExperienceId = experience.Id,
            Title = request.Title.Trim(),
            Slug = BuildSlug(request.Title),
            Description = request.Description.Trim(),
            Responsibilities = request.Responsibilities?.Trim(),
            Requirements = request.Requirements?.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? null : request.EmploymentType.Trim(),
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
            ExpiresAt = request.ExpiresAt,
            Status = request.Status ?? JobStatus.Draft,
            PublishedAt = request.Status == JobStatus.Published ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Jobs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await MapSummaryAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<JobSummaryDto>> GetEmployerJobsAsync(Guid employerUserId, CancellationToken cancellationToken)
    {
        var employerProfileId = await _dbContext.EmployerProfiles
            .Where(x => x.UserId == employerUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfileId == Guid.Empty)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        return await _dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.EmployerProfileId == employerProfileId && !x.IsDeleted)
            .Include(x => x.Category)
            .Include(x => x.Experience)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new JobSummaryDto(
                x.Id,
                x.Title,
                x.Location,
                x.EmploymentType,
                x.Category.Name,
                x.Experience.Name,
                x.SalaryMin,
                x.SalaryMax,
                x.Currency,
                x.Status,
                x.CreatedAt,
                x.PublishedAt,
                x.ExpiresAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<JobDetailDto> GetEmployerJobAsync(Guid employerUserId, Guid jobId, CancellationToken cancellationToken)
    {
        var employerProfileId = await _dbContext.EmployerProfiles
            .Where(x => x.UserId == employerUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfileId == Guid.Empty)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        var job = await _dbContext.Jobs
            .Include(x => x.Category)
            .Include(x => x.Experience)
            .Include(x => x.EmployerProfile)
            .FirstOrDefaultAsync(x => x.Id == jobId && x.EmployerProfileId == employerProfileId && !x.IsDeleted, cancellationToken)
            ?? throw new AuthException("Job not found.", 404);

        return new JobDetailDto(
            job.Id,
            job.Title,
            job.Description,
            job.EmployerProfile.CompanyName,
            job.Location,
            job.EmploymentType,
            job.Category.Name,
            job.Experience.Name,
            job.SalaryMin,
            job.SalaryMax,
            job.Currency,
            job.Responsibilities,
            job.Requirements,
            job.CreatedAt,
            job.PublishedAt,
            job.ExpiresAt,
            job.Status);
    }

    public async Task<JobSummaryDto> UpdateAsync(Guid employerUserId, Guid jobId, UpdateJobRequest request, CancellationToken cancellationToken)
    {
        var job = await LoadOwnedJobAsync(employerUserId, jobId, cancellationToken);
        ValidateUpdatableFields(request);

        if (request.Title is not null) { job.Title = request.Title.Trim(); job.Slug = BuildSlug(request.Title); }
        if (request.Description is not null) { job.Description = request.Description.Trim(); }
        if (request.CategoryId.HasValue) { job.CategoryId = request.CategoryId.Value; }
        if (request.ExperienceId.HasValue) { job.ExperienceId = request.ExperienceId.Value; }
        if (request.Location is not null) { job.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(); }
        if (request.EmploymentType is not null) { job.EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? null : request.EmploymentType.Trim(); }
        if (request.SalaryMin.HasValue) { job.SalaryMin = request.SalaryMin; }
        if (request.SalaryMax.HasValue) { job.SalaryMax = request.SalaryMax; }
        if (request.Currency is not null) { job.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(); }
        if (request.Responsibilities is not null) { job.Responsibilities = request.Responsibilities.Trim(); }
        if (request.Requirements is not null) { job.Requirements = request.Requirements.Trim(); }
        if (request.ExpiresAt.HasValue) { job.ExpiresAt = request.ExpiresAt.Value; }

        if (job.SalaryMin.HasValue && job.SalaryMax.HasValue && job.SalaryMin > job.SalaryMax)
        {
            throw new AuthException("Minimum salary cannot be greater than maximum salary.", 400);
        }

        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await MapSummaryAsync(job, cancellationToken);
    }

    public async Task<JobSummaryDto> UpdateStatusAsync(Guid employerUserId, Guid jobId, JobStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var job = await LoadOwnedJobAsync(employerUserId, jobId, cancellationToken);
        var currentStatus = job.Status;
        if (!Job.IsTransitionAllowed(currentStatus, request.Status))
        {
            throw new AuthException($"Invalid status transition from {currentStatus} to {request.Status}.", 400);
        }

        job.Status = request.Status;
        if (request.Status == JobStatus.Published && !job.PublishedAt.HasValue)
        {
            job.PublishedAt = DateTime.UtcNow;
        }
        if (request.Status == JobStatus.Closed || request.Status == JobStatus.Archived)
        {
            job.ExpiresAt ??= DateTime.UtcNow;
        }

        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await MapSummaryAsync(job, cancellationToken);
    }

    public async Task DeleteDraftAsync(Guid employerUserId, Guid jobId, CancellationToken cancellationToken)
    {
        var job = await LoadOwnedJobAsync(employerUserId, jobId, cancellationToken);
        if (job.Status != JobStatus.Draft)
        {
            throw new AuthException("Only draft jobs can be deleted.", 400);
        }

        job.IsDeleted = true;
        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<JobSummaryDto>> SearchPublicAsync(JobSearchRequest request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var query = _dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.Status == JobStatus.Published && !x.IsDeleted && x.PublishedAt != null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.Title.Contains(search) ||
                x.Description.Contains(search) ||
                (x.Requirements != null && x.Requirements.Contains(search)) ||
                (x.Responsibilities != null && x.Responsibilities.Contains(search)));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (request.ExperienceId.HasValue)
        {
            query = query.Where(x => x.ExperienceId == request.ExperienceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(x => x.Location != null && x.Location.Contains(request.Location.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(request.EmploymentType))
        {
            query = query.Where(x => x.EmploymentType != null && x.EmploymentType == request.EmploymentType.Trim());
        }

        if (request.MinSalary.HasValue)
        {
            query = query.Where(x => x.SalaryMax == null || x.SalaryMax >= request.MinSalary.Value || x.SalaryMin >= request.MinSalary.Value);
        }

        if (request.MaxSalary.HasValue)
        {
            query = query.Where(x => x.SalaryMin == null || x.SalaryMin <= request.MaxSalary.Value || x.SalaryMax <= request.MaxSalary.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = (request.Sort?.ToLowerInvariant()) switch
        {
            "salary-desc" => query.OrderByDescending(x => x.SalaryMax ?? x.SalaryMin ?? 0),
            "salary-asc" => query.OrderBy(x => x.SalaryMin ?? x.SalaryMax ?? 0),
            "newest" => query.OrderByDescending(x => x.PublishedAt),
            "oldest" => query.OrderBy(x => x.PublishedAt),
            _ => query.OrderByDescending(x => x.PublishedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new JobSummaryDto(
                x.Id,
                x.Title,
                x.Location,
                x.EmploymentType,
                x.Category.Name,
                x.Experience.Name,
                x.SalaryMin,
                x.SalaryMax,
                x.Currency,
                x.Status,
                x.CreatedAt,
                x.PublishedAt,
                x.ExpiresAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<JobSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<JobDetailDto> GetPublicAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs
            .Include(x => x.Category)
            .Include(x => x.Experience)
            .Include(x => x.EmployerProfile)
            .FirstOrDefaultAsync(x => x.Id == jobId && x.Status == JobStatus.Published && !x.IsDeleted && x.PublishedAt != null, cancellationToken)
            ?? throw new AuthException("Job not found.", 404);

        return new JobDetailDto(
            job.Id,
            job.Title,
            job.Description,
            job.EmployerProfile.CompanyName,
            job.Location,
            job.EmploymentType,
            job.Category.Name,
            job.Experience.Name,
            job.SalaryMin,
            job.SalaryMax,
            job.Currency,
            job.Responsibilities,
            job.Requirements,
            job.CreatedAt,
            job.PublishedAt,
            job.ExpiresAt,
            job.Status);
    }

    private async Task<Job> LoadOwnedJobAsync(Guid employerUserId, Guid jobId, CancellationToken cancellationToken)
    {
        var employerProfileId = await _dbContext.EmployerProfiles
            .Where(x => x.UserId == employerUserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (employerProfileId == Guid.Empty)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        return await _dbContext.Jobs
            .FirstOrDefaultAsync(x => x.Id == jobId && x.EmployerProfileId == employerProfileId && !x.IsDeleted, cancellationToken)
            ?? throw new AuthException("Job not found.", 404);
    }

    private async Task<JobSummaryDto> MapSummaryAsync(Job job, CancellationToken cancellationToken)
    {
        var category = await _dbContext.JobCategories.AsNoTracking().Where(x => x.Id == job.CategoryId).Select(x => x.Name).FirstAsync(cancellationToken);
        var experience = await _dbContext.JobExperiences.AsNoTracking().Where(x => x.Id == job.ExperienceId).Select(x => x.Name).FirstAsync(cancellationToken);

        return new JobSummaryDto(
            job.Id,
            job.Title,
            job.Location,
            job.EmploymentType,
            category,
            experience,
            job.SalaryMin,
            job.SalaryMax,
            job.Currency,
            job.Status,
            job.CreatedAt,
            job.PublishedAt,
            job.ExpiresAt);
    }

    private static string BuildSlug(string title)
    {
        var slug = title.Trim();
        slug = slug.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "[^a-z0-9]+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "job" : slug;
    }

    private static void ValidateRequest(CreateJobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new AuthException("Title is required.", 400);
        if (string.IsNullOrWhiteSpace(request.Description)) throw new AuthException("Description is required.", 400);
        if (request.Title.Length > 200) throw new AuthException("Title is too long.", 400);
        if (request.Description.Length > 20000) throw new AuthException("Description is too long.", 400);
        if (request.SalaryMin.HasValue && request.SalaryMin < 0m) throw new AuthException("Minimum salary cannot be negative.", 400);
        if (request.SalaryMax.HasValue && request.SalaryMax < 0m) throw new AuthException("Maximum salary cannot be negative.", 400);
        if (request.SalaryMin.HasValue && request.SalaryMax.HasValue && request.SalaryMin > request.SalaryMax) throw new AuthException("Minimum salary cannot be greater than maximum salary.", 400);
        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow) throw new AuthException("Application deadline must be in the future.", 400);
    }

    private static void ValidateUpdatableFields(UpdateJobRequest request)
    {
        if (request.Title is not null && string.IsNullOrWhiteSpace(request.Title)) throw new AuthException("Title is required.", 400);
        if (request.Description is not null && string.IsNullOrWhiteSpace(request.Description)) throw new AuthException("Description is required.", 400);
        if (request.Title is not null && request.Title.Length > 200) throw new AuthException("Title is too long.", 400);
        if (request.Description is not null && request.Description.Length > 20000) throw new AuthException("Description is too long.", 400);
        if (request.SalaryMin.HasValue && request.SalaryMin < 0m) throw new AuthException("Minimum salary cannot be negative.", 400);
        if (request.SalaryMax.HasValue && request.SalaryMax < 0m) throw new AuthException("Maximum salary cannot be negative.", 400);
        if (request.SalaryMin.HasValue && request.SalaryMax.HasValue && request.SalaryMin > request.SalaryMax) throw new AuthException("Minimum salary cannot be greater than maximum salary.", 400);
        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow) throw new AuthException("Application deadline must be in the future.", 400);
    }
}
