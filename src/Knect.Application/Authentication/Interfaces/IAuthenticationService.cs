using Knect.Application.Authentication.DTOs;

namespace Knect.Application.Authentication.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResultDto> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task<OtpResultDto> RequestOtpAsync(OtpRequest request, CancellationToken cancellationToken);
    Task<OtpResultDto> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<UserProfileDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IGoogleAuthenticationService
{
    Task<AuthResultDto> SignInWithGoogleAsync(GoogleSignInRequest request, CancellationToken cancellationToken);
}
