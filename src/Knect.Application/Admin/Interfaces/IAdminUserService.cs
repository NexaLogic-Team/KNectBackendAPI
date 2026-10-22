using Knect.Application.Admin.DTOs;

namespace Knect.Application.Admin.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserListItemDto>> ListUsersAsync(string? search, string? role, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminUserDetailDto> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<AdminUserDetailDto> UpdateUserStatusAsync(Guid adminUserId, Guid targetUserId, UserStatusUpdateRequest request, CancellationToken cancellationToken);
}
