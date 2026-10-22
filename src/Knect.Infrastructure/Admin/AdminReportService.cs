using Knect.Application.Admin.DTOs;
using Knect.Application.Admin.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Admin;

public sealed class AdminReportService : IAdminReportService
{
    private readonly KnectDbContext _dbContext;

    public AdminReportService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminReportRowDto>> GetUsersReportAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        ValidateDateRange(filter.From, filter.To);

        var query = _dbContext.Users.AsNoTracking().AsQueryable();
        if (filter.From.HasValue) query = query.Where(x => x.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.CreatedAt <= filter.To.Value);

        var rows = await query
            .GroupBy(x => x.Status)
            .Select(g => new AdminReportRowDto(g.Key.ToString(), g.Count(), null, null, null))
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken);

        return rows.Count == 0 ? new[] { new AdminReportRowDto("No users", 0) } : rows;
    }

    public async Task<IReadOnlyList<AdminReportRowDto>> GetJobsReportAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        ValidateDateRange(filter.From, filter.To);

        var query = _dbContext.Jobs.AsNoTracking().AsQueryable();
        if (filter.From.HasValue) query = query.Where(x => x.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.CreatedAt <= filter.To.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<JobStatus>(filter.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        var rows = await query
            .GroupBy(x => x.Status)
            .Select(g => new AdminReportRowDto(g.Key.ToString(), g.Count(), null, null, null))
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken);

        return rows.Count == 0 ? new[] { new AdminReportRowDto("No jobs", 0) } : rows;
    }

    public async Task<IReadOnlyList<AdminReportRowDto>> GetApplicationsReportAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        ValidateDateRange(filter.From, filter.To);

        var query = _dbContext.JobApplications.AsNoTracking().AsQueryable();
        if (filter.From.HasValue) query = query.Where(x => x.AppliedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.AppliedAt <= filter.To.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<ApplicationStatus>(filter.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        var rows = await query
            .GroupBy(x => x.Status)
            .Select(g => new AdminReportRowDto(g.Key.ToString(), g.Count(), null, null, null))
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken);

        return rows.Count == 0 ? new[] { new AdminReportRowDto("No applications", 0) } : rows;
    }

    public async Task<IReadOnlyList<AdminReportRowDto>> GetSubscriptionsReportAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        ValidateDateRange(filter.From, filter.To);

        var query = _dbContext.Subscriptions.AsNoTracking().AsQueryable();
        if (filter.From.HasValue) query = query.Where(x => x.StartDate >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.StartDate <= filter.To.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<SubscriptionStatus>(filter.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        var rows = await query
            .GroupBy(x => x.Status)
            .Select(g => new AdminReportRowDto(g.Key.ToString(), g.Count(), null, null, null))
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken);

        return rows.Count == 0 ? new[] { new AdminReportRowDto("No subscriptions", 0) } : rows;
    }

    public async Task<IReadOnlyList<AdminReportRowDto>> GetPaymentsReportAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        ValidateDateRange(filter.From, filter.To);

        var query = _dbContext.PaymentRecords.AsNoTracking().AsQueryable();
        if (filter.From.HasValue) query = query.Where(x => x.SubmittedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.SubmittedAt <= filter.To.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<PaymentStatus>(filter.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        var rows = await query
            .GroupBy(x => x.Status)
            .Select(g => new AdminReportRowDto(g.Key.ToString(), g.Count(), g.Sum(x => x.Amount), g.First().Currency, null))
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken);

        return rows.Count == 0 ? new[] { new AdminReportRowDto("No payments", 0, 0m, "USD") } : rows;
    }

    public static void ValidateDateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new AuthException("The start date must be less than or equal to the end date.", 400);
        }
    }
}
