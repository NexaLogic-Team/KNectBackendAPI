using Knect.Application.Admin.DTOs;
using Knect.Application.Admin.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Admin;

public sealed class AdminEmployerService : IAdminEmployerService
{
    private readonly KnectDbContext _dbContext;

    public AdminEmployerService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AdminEmployerListItemDto>> ListEmployersAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.EmployerProfile)
            .Include(x => x.JobApplications)
            .Where(x => x.UserRoles.Any(ur => ur.Role.Name == "Employer"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.EmployerProfile != null && x.EmployerProfile.CompanyName.Contains(term) || x.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new AdminEmployerListItemDto(
                x.Id,
                x.EmployerProfile != null ? x.EmployerProfile.CompanyName : "",
                x.Email,
                x.Status,
                x.CreatedAt,
                x.EmployerProfile != null ? x.EmployerProfile.Jobs.Count : 0,
                x.JobApplications.Count,
                x.EmployerProfile != null && x.EmployerProfile.IsVerified))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminEmployerListItemDto>(items, safePage, safePageSize, totalCount);
    }

    public async Task<AdminEmployerDetailDto> GetEmployerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.EmployerProfile)
            .Include(x => x.Subscriptions)
            .ThenInclude(x => x.Plan)
            .Include(x => x.JobApplications)
            .ThenInclude(x => x.Job)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AuthException("Employer not found.", 404);

        var isEmployer = user.UserRoles.Any(x => x.Role.Name == "Employer");
        if (!isEmployer)
        {
            throw new AuthException("User is not an employer.", 400);
        }

        var profile = user.EmployerProfile ?? throw new AuthException("Employer profile not found.", 404);

        return new AdminEmployerDetailDto(
            user.Id,
            profile.CompanyName,
            profile.CompanyDescription,
            profile.Industry,
            profile.Website,
            profile.Address,
            profile.City,
            profile.Country,
            profile.ContactEmail,
            profile.ContactPhone,
            user.Status,
            profile.IsVerified,
            user.CreatedAt,
            profile.Jobs.Count,
            user.JobApplications.Count,
            user.Subscriptions.Count,
            profile.Payments.Count);
    }

    public async Task<AdminEmployerDetailDto> UpdateEmployerStatusAsync(Guid adminUserId, Guid userId, EmployerStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken)
            ?? throw new AuthException("Admin user not found.", 404);

        if (!admin.UserRoles.Any(x => x.Role.Name == "Admin"))
        {
            throw new AuthException("Admin privileges are required.", 403);
        }

        var user = await _dbContext.Users
            .Include(x => x.EmployerProfile)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AuthException("Employer not found.", 404);

        if (!user.UserRoles.Any(x => x.Role.Name == "Employer"))
        {
            throw new AuthException("User is not an employer.", 400);
        }

        user.Status = request.Status;
        user.UpdatedAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            EventType = "EmployerStatusChanged",
            Message = $"Admin changed employer status to {request.Status}.",
            DetailsJson = $"{{\"employerUserId\":\"{userId}\",\"status\":\"{request.Status}\",\"reason\":\"{(request.Reason ?? string.Empty)}\"}}",
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetEmployerAsync(userId, cancellationToken);
    }
}
