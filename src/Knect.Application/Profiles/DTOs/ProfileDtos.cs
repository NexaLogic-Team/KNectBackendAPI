using Knect.Domain.Enums;

namespace Knect.Application.Profiles.DTOs;

public sealed record UpdateUserProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Location,
    string? ProfessionalSummary,
    string? Skills,
    string? WorkExperience,
    string? Education,
    string? ProfileImageUrl,
    string? CvFileName);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Location,
    string? ProfessionalSummary,
    string? Skills,
    string? WorkExperience,
    string? Education,
    bool IsEmailVerified,
    UserStatus Status,
    string? ProfileImageUrl,
    string? CvFileKey,
    IReadOnlyCollection<string> Roles,
    bool IsPrivateView);

public sealed record PublicUserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Location,
    string? ProfessionalSummary,
    string? Skills,
    string? ProfileImageUrl,
    bool IsPrivateView);

public sealed record UpdateEmployerProfileRequest(
    string? CompanyName,
    string? CompanyDescription,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Address,
    string? City,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    string? LogoUrl);

public sealed record EmployerProfileDto(
    Guid Id,
    Guid UserId,
    string CompanyName,
    string? CompanyDescription,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Address,
    string? City,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    string? LogoUrl,
    bool IsVerified,
    bool IsPrivateView);
