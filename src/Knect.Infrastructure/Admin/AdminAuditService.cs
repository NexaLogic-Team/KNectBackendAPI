using Knect.Application.Admin.DTOs;
using Knect.Application.Admin.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Admin;

public sealed class AdminAuditService : IAdminAuditService
{
    private readonly KnectDbContext _dbContext;

    public AdminAuditService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AdminAuditLogDto>> ListAsync(AuditLogFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 100);

        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (filter.UserId.HasValue) query = query.Where(x => x.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.EventType)) query = query.Where(x => x.EventType.Contains(filter.EventType.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.EntityType)) query = query.Where(x => x.DetailsJson != null && x.DetailsJson.Contains(filter.EntityType.Trim()));
        if (filter.EntityId.HasValue) query = query.Where(x => x.DetailsJson != null && x.DetailsJson.Contains(filter.EntityId.Value.ToString()));
        if (filter.From.HasValue) query = query.Where(x => x.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.CreatedAt <= filter.To.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminAuditLogDto(
                x.Id,
                x.UserId,
                x.EventType,
                x.Message,
                x.DetailsJson,
                x.IpAddress,
                x.CorrelationId,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminAuditLogDto>(items, page, pageSize, total);
    }

    public async Task<AdminAuditLogDto> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken)
    {
        var log = await _dbContext.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == auditLogId, cancellationToken)
            ?? throw new AuthException("Audit log not found.", 404);

        return new AdminAuditLogDto(
            log.Id,
            log.UserId,
            log.EventType,
            log.Message,
            log.DetailsJson,
            log.IpAddress,
            log.CorrelationId,
            log.CreatedAt);
    }
}
