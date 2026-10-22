using Knect.Domain.Enums;

namespace Knect.Application.Applications.DTOs;

public sealed record ApplyToJobRequest(
    string? CoverLetter,
    string? CvFileKey,
    string? CvFileName);

public sealed record ApplicationSummaryDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    ApplicationStatus Status,
    DateTime AppliedAt,
    DateTime? UpdatedAt,
    bool IsWithdrawn);

public sealed record ApplicationDetailDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    ApplicationStatus Status,
    string? CoverLetter,
    string? CvFileKey,
    DateTime AppliedAt,
    DateTime? UpdatedAt,
    bool IsWithdrawn,
    string? CandidateName,
    string? CandidateEmail,
    bool IsPrivateView);

public sealed record UpdateApplicationStatusRequest(ApplicationStatus Status);

public sealed record EmployerApplicationFilter(
    Guid? JobId,
    ApplicationStatus? Status,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
