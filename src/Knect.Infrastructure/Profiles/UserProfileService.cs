using Knect.Application.Authentication.Exceptions;
using Knect.Application.Profiles.DTOs;
using Knect.Application.Profiles.Interfaces;
using Knect.Application.Profiles.Validators;
using Knect.Domain.Entities;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Profiles;

public sealed class UserProfileService : IUserProfileService
{
    private readonly KnectDbContext _dbContext;

    public UserProfileService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        return MapUser(user, true);
    }

    public async Task<UserProfileDto> UpsertAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        var validator = new UserProfileValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new AuthException(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage)), 400);
        }

        user.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? user.FirstName : request.FirstName.Trim();
        user.LastName = string.IsNullOrWhiteSpace(request.LastName) ? user.LastName : request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? user.PhoneNumber : request.PhoneNumber.Trim();
        user.Location = string.IsNullOrWhiteSpace(request.Location) ? user.Location : request.Location.Trim();
        user.ProfessionalSummary = string.IsNullOrWhiteSpace(request.ProfessionalSummary) ? user.ProfessionalSummary : request.ProfessionalSummary.Trim();
        user.Skills = string.IsNullOrWhiteSpace(request.Skills) ? user.Skills : request.Skills.Trim();
        user.WorkExperience = string.IsNullOrWhiteSpace(request.WorkExperience) ? user.WorkExperience : request.WorkExperience.Trim();
        user.Education = string.IsNullOrWhiteSpace(request.Education) ? user.Education : request.Education.Trim();
        user.ProfileImageUrl = string.IsNullOrWhiteSpace(request.ProfileImageUrl) ? user.ProfileImageUrl : request.ProfileImageUrl.Trim();
        user.CvFileName = string.IsNullOrWhiteSpace(request.CvFileName) ? user.CvFileName : request.CvFileName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapUser(user, true);
    }

    public async Task<PublicUserProfileDto> GetPublicAsync(Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var target = await LoadUserAsync(targetUserId, cancellationToken);
        var isSelf = targetUserId == currentUserId;
        if (isSelf)
        {
            return new PublicUserProfileDto(target.Id, target.FirstName, target.LastName, target.Location, target.ProfessionalSummary, target.Skills, target.ProfileImageUrl, false);
        }

        return new PublicUserProfileDto(target.Id, target.FirstName, target.LastName, target.Location, target.ProfessionalSummary, target.Skills, target.ProfileImageUrl, true);
    }

    private async Task<User> LoadUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new AuthException("User not found.", 404);
        }

        return user;
    }

    private static UserProfileDto MapUser(User user, bool includePrivate)
    {
        var roles = user.UserRoles.Select(x => x.Role.Name).ToArray();
        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Location,
            user.ProfessionalSummary,
            user.Skills,
            user.WorkExperience,
            user.Education,
            user.IsEmailVerified,
            user.Status,
            user.ProfileImageUrl,
            includePrivate ? user.CvFileKey : null,
            roles,
            includePrivate);
    }
}
