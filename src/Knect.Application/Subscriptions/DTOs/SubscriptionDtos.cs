using Knect.Domain.Enums;

namespace Knect.Application.Subscriptions.DTOs;

public sealed record CreateSubscriptionPlanRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int DurationDays,
    int MaxJobPosts,
    bool IsActive = true);

public sealed record UpdateSubscriptionPlanRequest(
    string? Name,
    string? Description,
    decimal? Price,
    string? Currency,
    int? DurationDays,
    int? MaxJobPosts,
    bool? IsActive);

public sealed record SubscriptionPlanDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int DurationDays,
    int MaxJobPosts,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record EmployerSubscriptionPurchaseRequest(
    Guid PlanId,
    string PaymentMethod,
    string PaymentReference,
    string? EvidenceFileKey,
    string? Notes,
    bool AutoRenew = false);

public sealed record SubscriptionSummaryDto(
    Guid Id,
    Guid PlanId,
    string PlanName,
    decimal PlanPrice,
    string Currency,
    int DurationDays,
    int MaxJobPosts,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    bool AutoRenew,
    string? Notes,
    DateTime CreatedAt);

public sealed record PaymentRecordDto(
    Guid Id,
    Guid EmployerProfileId,
    Guid? SubscriptionId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string PaymentReference,
    string? EvidenceFileKey,
    PaymentStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    Guid? ReviewedByAdminId,
    string? ReviewedByAdminName);

public sealed record ReviewPaymentRequest(
    PaymentStatus Status,
    string? ReviewNotes);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
