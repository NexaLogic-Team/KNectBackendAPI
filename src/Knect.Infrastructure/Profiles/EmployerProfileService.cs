using Knect.Application.Authentication.Exceptions;
using Knect.Application.Profiles.DTOs;
using Knect.Application.Profiles.Interfaces;
using Knect.Application.Profiles.Validators;
using Knect.Domain.Entities;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Profiles;

public sealed class EmployerProfileService : IEmployerProfileService
{
    private readonly KnectDbContext _dbContext;

    public EmployerProfileService(KnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployerProfileDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await LoadCurrentProfileAsync(userId, cancellationToken);
        return Map(profile, true);
    }

    public async Task<EmployerProfileDto> UpsertAsync(Guid userId, UpdateEmployerProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new AuthException("User not found.", 404);
        }

        var validator = new EmployerProfileValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new AuthException(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage)), 400);
        }

        var profile = await _dbContext.EmployerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new EmployerProfile
            {
                UserId = userId,
                CompanyName = request.CompanyName?.Trim() ?? string.Empty,
                CompanyDescription = request.CompanyDescription?.Trim(),
                Industry = request.Industry?.Trim(),
                CompanySize = request.CompanySize?.Trim(),
                Website = request.Website?.Trim(),
                Address = request.Address?.Trim(),
                City = request.City?.Trim(),
                Country = request.Country?.Trim(),
                ContactEmail = request.ContactEmail?.Trim(),
                ContactPhone = request.ContactPhone?.Trim(),
                LogoUrl = request.LogoUrl?.Trim(),
                IsVerified = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.EmployerProfiles.Add(profile);
        }
        else
        {
            profile.CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? profile.CompanyName : request.CompanyName.Trim();
            profile.CompanyDescription = string.IsNullOrWhiteSpace(request.CompanyDescription) ? profile.CompanyDescription : request.CompanyDescription.Trim();
            profile.Industry = string.IsNullOrWhiteSpace(request.Industry) ? profile.Industry : request.Industry.Trim();
            profile.CompanySize = string.IsNullOrWhiteSpace(request.CompanySize) ? profile.CompanySize : request.CompanySize.Trim();
            profile.Website = string.IsNullOrWhiteSpace(request.Website) ? profile.Website : request.Website.Trim();
            profile.Address = string.IsNullOrWhiteSpace(request.Address) ? profile.Address : request.Address.Trim();
            profile.City = string.IsNullOrWhiteSpace(request.City) ? profile.City : request.City.Trim();
            profile.Country = string.IsNullOrWhiteSpace(request.Country) ? profile.Country : request.Country.Trim();
            profile.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? profile.ContactEmail : request.ContactEmail.Trim();
            profile.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? profile.ContactPhone : request.ContactPhone.Trim();
            profile.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? profile.LogoUrl : request.LogoUrl.Trim();
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(profile, true);
    }

    public async Task<EmployerProfileDto> GetPublicAsync(Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var profile = await LoadCurrentProfileAsync(targetUserId, cancellationToken);
        return Map(profile, currentUserId == targetUserId);
    }

    private async Task<EmployerProfile> LoadCurrentProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _dbContext.EmployerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            throw new AuthException("Employer profile not found.", 404);
        }

        return profile;
    }

    private static EmployerProfileDto Map(EmployerProfile profile, bool isPrivateView)
        => new(
            profile.Id,
            profile.UserId,
            profile.CompanyName,
            profile.CompanyDescription,
            profile.Industry,
            profile.CompanySize,
            profile.Website,
            profile.Address,
            profile.City,
            profile.Country,
            profile.ContactEmail,
            profile.ContactPhone,
            profile.LogoUrl,
            profile.IsVerified,
            isPrivateView);
}
