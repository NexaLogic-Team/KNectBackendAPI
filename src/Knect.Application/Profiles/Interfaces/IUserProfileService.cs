using Knect.Application.Profiles.DTOs;

namespace Knect.Application.Profiles.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserProfileDto> UpsertAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken);
    Task<PublicUserProfileDto> GetPublicAsync(Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken);
}
