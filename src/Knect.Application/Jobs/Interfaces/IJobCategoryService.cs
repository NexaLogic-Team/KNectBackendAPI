using Knect.Application.Jobs.DTOs;

namespace Knect.Application.Jobs.Interfaces;

public interface IJobCategoryService
{
    Task<IReadOnlyList<JobReferenceDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<JobReferenceDto> CreateAsync(string name, string? description, CancellationToken cancellationToken);
    Task<JobReferenceDto> UpdateAsync(Guid categoryId, string name, string? description, bool? isActive, CancellationToken cancellationToken);
    Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken);
}

public interface IJobExperienceService
{
    Task<IReadOnlyList<JobReferenceDto>> GetAsync(CancellationToken cancellationToken);
    Task<JobReferenceDto> CreateAsync(string name, string? description, CancellationToken cancellationToken);
    Task<JobReferenceDto> UpdateAsync(Guid id, string name, string? description, bool? isActive, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
