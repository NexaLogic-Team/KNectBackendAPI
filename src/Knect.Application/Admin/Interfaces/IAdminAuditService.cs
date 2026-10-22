using Knect.Application.Admin.DTOs;

namespace Knect.Application.Admin.Interfaces;

public interface IAdminAuditService
{
    Task<PagedResult<AdminAuditLogDto>> ListAsync(AuditLogFilter filter, CancellationToken cancellationToken);
    Task<AdminAuditLogDto> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken);
}
