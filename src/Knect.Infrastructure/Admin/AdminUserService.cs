using Knect.Application.Admin.DTOs;
using Knect.Application.Admin.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Admin;

public sealed class AdminUserService : IAdminUserService
{
    private readonly KnectDbContext _dbContext;

    public AdminUserService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AdminUserListItemDto>> ListUsersAsync(string? search, string? role, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Email.Contains(term) || (x.FirstName + " " + x.LastName).Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalized = role.Trim();
            query = query.Where(x => x.UserRoles.Any(ur => ur.Role.Name == normalized));
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
            .Select(x => new AdminUserListItemDto(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.UserRoles.Any() ? x.UserRoles.First().Role.Name : "JobSeeker",
                x.Status,
                x.CreatedAt,
                x.IsEmailVerified))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserListItemDto>(items, safePage, safePageSize, totalCount);
    }

    public async Task<AdminUserDetailDto> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.EmployerProfile)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AuthException("User not found.", 404);

        var roleName = user.UserRoles.Select(x => x.Role.Name).FirstOrDefault() ?? "JobSeeker";

        return new AdminUserDetailDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            roleName,
            user.Status,
            user.IsEmailVerified,
            user.CreatedAt,
            user.LastLoginAt,
            user.EmployerProfile?.CompanyName,
            user.PhoneNumber,
            user.Location,
            user.ProfessionalSummary,
            user.Skills,
            user.WorkExperience,
            user.Education);
    }

    public async Task<AdminUserDetailDto> UpdateUserStatusAsync(Guid adminUserId, Guid targetUserId, UserStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        if (targetUserId == adminUserId)
        {
            throw new AuthException("An administrator cannot change their own account from this endpoint.", 400);
        }

        var targetUser = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == targetUserId, cancellationToken)
            ?? throw new AuthException("User not found.", 404);

        var admin = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken)
            ?? throw new AuthException("Admin user not found.", 404);

        var adminHasAdminRole = admin.UserRoles.Any(x => x.Role.Name == "Admin");
        if (!adminHasAdminRole)
        {
            throw new AuthException("Admin privileges are required.", 403);
        }

        if (targetUser.UserRoles.Any(x => x.Role.Name == "Admin") && request.Status != UserStatus.Active)
        {
            var adminCount = await _dbContext.UserRoles.CountAsync(x => x.Role.Name == "Admin" && x.User.Status == UserStatus.Active, cancellationToken);
            if (adminCount <= 1)
            {
                throw new AuthException("At least one active administrator must remain available.", 409);
            }
        }

        targetUser.Status = request.Status;
        targetUser.UpdatedAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            EventType = "UserStatusChanged",
            Message = $"Admin changed user status to {request.Status}.",
            DetailsJson = $"{{\"targetUserId\":\"{targetUserId}\",\"status\":\"{request.Status}\",\"reason\":\"{(request.Reason ?? string.Empty)}\"}}",
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(targetUserId, cancellationToken);
    }
}
