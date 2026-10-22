using Knect.Domain.Enums;

namespace Knect.Application.Jobs.DTOs;

public sealed record CreateJobRequest(
    string Title,
    string Description,
    Guid CategoryId,
    Guid ExperienceId,
    string? Location,
    string? EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? Currency,
    string? Responsibilities,
    string? Requirements,
    DateTime? ExpiresAt,
    JobStatus? Status = null);

public sealed record UpdateJobRequest(
    string? Title,
    string? Description,
    Guid? CategoryId,
    Guid? ExperienceId,
    string? Location,
    string? EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? Currency,
    string? Responsibilities,
    string? Requirements,
    DateTime? ExpiresAt);

public sealed record JobStatusUpdateRequest(JobStatus Status);

public sealed record JobReferenceDto(Guid Id, string Name, string? Description, bool IsActive);

public sealed record JobSummaryDto(
    Guid Id,
    string Title,
    string? Location,
    string? EmploymentType,
    string CategoryName,
    string ExperienceName,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Currency,
    JobStatus Status,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    DateTime? ExpiresAt);

public sealed record JobDetailDto(
    Guid Id,
    string Title,
    string Description,
    string CompanyName,
    string? Location,
    string? EmploymentType,
    string CategoryName,
    string ExperienceName,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Currency,
    string? Responsibilities,
    string? Requirements,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    DateTime? ExpiresAt,
    JobStatus Status);

public sealed record JobSearchRequest(
    string? Search,
    Guid? CategoryId,
    Guid? ExperienceId,
    string? Location,
    string? EmploymentType,
    decimal? MinSalary,
    decimal? MaxSalary,
    string? Sort,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
