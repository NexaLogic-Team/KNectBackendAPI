using Knect.Application.Authentication.Exceptions;
using Knect.Application.Jobs.DTOs;
using Knect.Application.Jobs.Interfaces;
using Knect.Domain.Entities;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Jobs;

public sealed class JobCategoryService : IJobCategoryService
{
    private readonly KnectDbContext _dbContext;

    public JobCategoryService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<JobReferenceDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.JobCategories
            .OrderBy(x => x.Name)
            .Select(x => new JobReferenceDto(x.Id, x.Name, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<JobReferenceDto> CreateAsync(string name, string? description, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(name);
        if (await _dbContext.JobCategories.AnyAsync(x => x.Name == normalizedName, cancellationToken))
        {
            throw new AuthException("A category with this name already exists.", 409);
        }

        var entity = new JobCategory
        {
            Name = normalizedName,
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.JobCategories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new JobReferenceDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }

    public async Task<JobReferenceDto> UpdateAsync(Guid categoryId, string name, string? description, bool? isActive, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.JobCategories.FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken)
            ?? throw new AuthException("Category not found.", 404);

        var normalizedName = NormalizeName(name);
        if (entity.Name != normalizedName && await _dbContext.JobCategories.AnyAsync(x => x.Id != categoryId && x.Name == normalizedName, cancellationToken))
        {
            throw new AuthException("A category with this name already exists.", 409);
        }

        entity.Name = normalizedName;
        entity.Description = description?.Trim();
        if (isActive.HasValue)
        {
            entity.IsActive = isActive.Value;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new JobReferenceDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }

    public async Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.JobCategories.FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken)
            ?? throw new AuthException("Category not found.", 404);

        _dbContext.JobCategories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeName(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Name is required.", 400);
        }

        if (normalized.Length > 120)
        {
            throw new AuthException("Name is too long.", 400);
        }

        return normalized;
    }
}
