using Knect.Domain.Enums;

namespace Knect.Application.Admin.DTOs;

public sealed record DashboardSummaryDto(
    int TotalUsers,
    int TotalJobSeekers,
    int TotalEmployers,
    int ActiveUsers,
    int InactiveUsers,
    int TotalJobs,
    int PublishedJobs,
    int DraftJobs,
    int ClosedJobs,
    int ExpiredJobs,
    int TotalApplications,
    int SubmittedApplications,
    int UnderReviewApplications,
    int ShortlistedApplications,
    int AcceptedApplications,
    int RejectedApplications,
    int ActiveSubscriptions,
    int ExpiredSubscriptions,
    int PendingSubscriptions,
    int PendingPayments,
    int ConfirmedPayments,
    int RejectedPayments,
    IReadOnlyList<RecentRegistrationDto> RecentRegistrations,
    IReadOnlyList<RecentJobDto> RecentJobs,
    IReadOnlyList<RecentPaymentReviewDto> RecentPaymentReviews);

public sealed record RecentRegistrationDto(Guid UserId, string Email, string FullName, DateTime CreatedAt);
public sealed record RecentJobDto(Guid JobId, string Title, string CompanyName, DateTime CreatedAt, JobStatus Status);
public sealed record RecentPaymentReviewDto(Guid PaymentId, string CompanyName, decimal Amount, string Currency, PaymentStatus Status, DateTime SubmittedAt);

public sealed record AdminUserListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    UserStatus Status,
    DateTime CreatedAt,
    bool IsEmailVerified);

public sealed record AdminUserDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    UserStatus Status,
    bool IsEmailVerified,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string? CompanyName,
    string? PhoneNumber,
    string? Location,
    string? ProfessionalSummary,
    string? Skills,
    string? WorkExperience,
    string? Education);

public sealed record AdminEmployerListItemDto(
    Guid UserId,
    string CompanyName,
    string Email,
    UserStatus Status,
    DateTime CreatedAt,
    int JobsCount,
    int ApplicationsCount,
    bool IsVerified);

public sealed record AdminEmployerDetailDto(
    Guid UserId,
    string CompanyName,
    string? CompanyDescription,
    string? Industry,
    string? Website,
    string? Address,
    string? City,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    UserStatus Status,
    bool IsVerified,
    DateTime CreatedAt,
    int TotalJobs,
    int TotalApplications,
    int TotalSubscriptions,
    int TotalPayments);

public sealed record AdminAuditLogDto(
    Guid Id,
    Guid? UserId,
    string EventType,
    string Message,
    string? DetailsJson,
    string? IpAddress,
    string? CorrelationId,
    DateTime CreatedAt);

public sealed record UserStatusUpdateRequest(UserStatus Status, string? Reason = null);
public sealed record EmployerStatusUpdateRequest(UserStatus Status, string? Reason = null);
public sealed record AuditLogFilter(
    Guid? UserId,
    string? EventType,
    string? EntityType,
    Guid? EntityId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20);

public sealed record ReportFilter(
    DateTime? From,
    DateTime? To,
    string? Status,
    string? Category,
    Guid? EmployerId,
    int Page = 1,
    int PageSize = 20);

public sealed record AdminReportRowDto(string Label, int Count, decimal? Amount = null, string? Currency = null, DateTime? Date = null);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
