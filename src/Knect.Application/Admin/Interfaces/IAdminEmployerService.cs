using Knect.Application.Admin.DTOs;

namespace Knect.Application.Admin.Interfaces;

public interface IAdminEmployerService
{
    Task<PagedResult<AdminEmployerListItemDto>> ListEmployersAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminEmployerDetailDto> GetEmployerAsync(Guid userId, CancellationToken cancellationToken);
    Task<AdminEmployerDetailDto> UpdateEmployerStatusAsync(Guid adminUserId, Guid userId, EmployerStatusUpdateRequest request, CancellationToken cancellationToken);
}
