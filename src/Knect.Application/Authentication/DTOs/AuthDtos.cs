using Knect.Domain.Enums;

namespace Knect.Application.Authentication.DTOs;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role = "JobSeeker");

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record OtpRequest(string Email, string Purpose = "EmailVerification");
public sealed record VerifyOtpRequest(string Email, string Purpose, string Code);
public sealed record ResetPasswordRequest(string Email, string Purpose, string Code, string NewPassword);
public sealed record GoogleSignInRequest(string Credential);

public sealed record AuthResultDto(
    Guid UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    IReadOnlyCollection<string> Roles);

public sealed record OtpResultDto(bool Success, string Message);

public sealed record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsEmailVerified,
    UserStatus Status,
    IReadOnlyCollection<string> Roles);
