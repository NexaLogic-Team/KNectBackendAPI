using Knect.Application.Profiles.DTOs;

namespace Knect.Application.Profiles.Interfaces;

public interface IEmployerProfileService
{
    Task<EmployerProfileDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);
    Task<EmployerProfileDto> UpsertAsync(Guid userId, UpdateEmployerProfileRequest request, CancellationToken cancellationToken);
    Task<EmployerProfileDto> GetPublicAsync(Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken);
}
