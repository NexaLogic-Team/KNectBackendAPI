using Knect.Application.Admin.DTOs;
using Knect.Application.Admin.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Admin;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly KnectDbContext _dbContext;

    public AdminDashboardService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var userQuery = _dbContext.Users.AsNoTracking();
        var jobQuery = _dbContext.Jobs.AsNoTracking();
        var applicationQuery = _dbContext.JobApplications.AsNoTracking();
        var subscriptionQuery = _dbContext.Subscriptions.AsNoTracking();
        var paymentQuery = _dbContext.PaymentRecords.AsNoTracking();

        var totalUsers = await userQuery.CountAsync(cancellationToken);
        var totalJobSeekers = await userQuery.CountAsync(x => x.UserRoles.Any(ur => ur.Role.Name == "JobSeeker"), cancellationToken);
        var totalEmployers = await userQuery.CountAsync(x => x.UserRoles.Any(ur => ur.Role.Name == "Employer"), cancellationToken);
        var activeUsers = await userQuery.CountAsync(x => x.Status == UserStatus.Active, cancellationToken);
        var inactiveUsers = await userQuery.CountAsync(x => x.Status != UserStatus.Active, cancellationToken);

        var totalJobs = await jobQuery.CountAsync(cancellationToken);
        var publishedJobs = await jobQuery.CountAsync(x => x.Status == JobStatus.Published, cancellationToken);
        var draftJobs = await jobQuery.CountAsync(x => x.Status == JobStatus.Draft, cancellationToken);
        var closedJobs = await jobQuery.CountAsync(x => x.Status == JobStatus.Closed, cancellationToken);
        var expiredJobs = await jobQuery.CountAsync(x => x.Status == JobStatus.Published && x.ExpiresAt != null && x.ExpiresAt <= now, cancellationToken);

        var totalApplications = await applicationQuery.CountAsync(cancellationToken);
        var submittedApplications = await applicationQuery.CountAsync(x => x.Status == ApplicationStatus.Submitted, cancellationToken);
        var underReviewApplications = await applicationQuery.CountAsync(x => x.Status == ApplicationStatus.UnderReview, cancellationToken);
        var shortlistedApplications = await applicationQuery.CountAsync(x => x.Status == ApplicationStatus.Shortlisted, cancellationToken);
        var acceptedApplications = await applicationQuery.CountAsync(x => x.Status == ApplicationStatus.Accepted, cancellationToken);
        var rejectedApplications = await applicationQuery.CountAsync(x => x.Status == ApplicationStatus.Rejected, cancellationToken);

        var activeSubscriptions = await subscriptionQuery.CountAsync(x => x.Status == SubscriptionStatus.Active && x.EndDate > now, cancellationToken);
        var expiredSubscriptions = await subscriptionQuery.CountAsync(x => x.Status == SubscriptionStatus.Expired || x.EndDate <= now, cancellationToken);
        var pendingSubscriptions = await subscriptionQuery.CountAsync(x => x.Status == SubscriptionStatus.Pending, cancellationToken);

        var pendingPayments = await paymentQuery.CountAsync(x => x.Status == PaymentStatus.Pending, cancellationToken);
        var confirmedPayments = await paymentQuery.CountAsync(x => x.Status == PaymentStatus.Confirmed, cancellationToken);
        var rejectedPayments = await paymentQuery.CountAsync(x => x.Status == PaymentStatus.Rejected, cancellationToken);

        var recentRegistrations = await userQuery
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new RecentRegistrationDto(x.Id, x.Email, x.FirstName + " " + x.LastName, x.CreatedAt))
            .ToListAsync(cancellationToken);

        var recentJobs = await jobQuery
            .Include(x => x.EmployerProfile)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new RecentJobDto(x.Id, x.Title, x.EmployerProfile.CompanyName, x.CreatedAt, x.Status))
            .ToListAsync(cancellationToken);

        var recentPaymentReviews = await paymentQuery
            .Include(x => x.EmployerProfile)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(10)
            .Select(x => new RecentPaymentReviewDto(x.Id, x.EmployerProfile.CompanyName, x.Amount, x.Currency, x.Status, x.SubmittedAt))
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto(
            totalUsers,
            totalJobSeekers,
            totalEmployers,
            activeUsers,
            inactiveUsers,
            totalJobs,
            publishedJobs,
            draftJobs,
            closedJobs,
            expiredJobs,
            totalApplications,
            submittedApplications,
            underReviewApplications,
            shortlistedApplications,
            acceptedApplications,
            rejectedApplications,
            activeSubscriptions,
            expiredSubscriptions,
            pendingSubscriptions,
            pendingPayments,
            confirmedPayments,
            rejectedPayments,
            recentRegistrations,
            recentJobs,
            recentPaymentReviews);
    }

    public async Task<PagedResult<RecentRegistrationDto>> GetRecentRegistrationsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.Users.AsNoTracking().OrderByDescending(x => x.CreatedAt)
            .Select(x => new RecentRegistrationDto(x.Id, x.Email, x.FirstName + " " + x.LastName, x.CreatedAt));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToListAsync(cancellationToken);
        return new PagedResult<RecentRegistrationDto>(items, safePage, safePageSize, total);
    }

    public async Task<PagedResult<RecentJobDto>> GetRecentJobsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.Jobs.AsNoTracking()
            .Include(x => x.EmployerProfile)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RecentJobDto(x.Id, x.Title, x.EmployerProfile.CompanyName, x.CreatedAt, x.Status));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToListAsync(cancellationToken);
        return new PagedResult<RecentJobDto>(items, safePage, safePageSize, total);
    }

    public async Task<PagedResult<RecentPaymentReviewDto>> GetRecentPaymentsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.PaymentRecords.AsNoTracking()
            .Include(x => x.EmployerProfile)
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => new RecentPaymentReviewDto(x.Id, x.EmployerProfile.CompanyName, x.Amount, x.Currency, x.Status, x.SubmittedAt));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToListAsync(cancellationToken);
        return new PagedResult<RecentPaymentReviewDto>(items, safePage, safePageSize, total);
    }
}
