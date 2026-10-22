using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Knect.Application.Authentication;
using Knect.Application.Authentication.DTOs;
using Knect.Application.Authentication.Exceptions;
using Knect.Application.Authentication.Interfaces;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Knect.Infrastructure.Authentication;

public sealed class AuthenticationService : IAuthenticationService, IGoogleAuthenticationService
{
    private readonly KnectDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly GoogleAuthOptions _googleAuthOptions;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthenticationService(
        KnectDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        IOptions<GoogleAuthOptions> googleAuthOptions,
        IEmailSender emailSender,
        ILogger<AuthenticationService> logger)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
        _googleAuthOptions = googleAuthOptions.Value;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new AuthException("First name and last name are required.", 400);
        }

        var email = request.Email.Trim();
        if (!IsValidEmail(email))
        {
            throw new AuthException("A valid email address is required.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new AuthException("Password must be at least 8 characters long.", 400);
        }

        if (await _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new AuthException("A user with this email already exists.", 409);
        }

        var roleName = NormalizeRole(request.Role);
        if (roleName == "Admin")
        {
            throw new AuthException("Admin registration is not allowed in the public flow.", 403);
        }

        await EnsureRolesExistAsync(cancellationToken);

        var passwordUser = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            UserName = email,
            Status = UserStatus.Active,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            FirstName = passwordUser.FirstName,
            LastName = passwordUser.LastName,
            Email = passwordUser.Email,
            UserName = passwordUser.UserName,
            PasswordHash = _passwordHasher.HashPassword(passwordUser, request.Password),
            Status = UserStatus.Active,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var role = await _dbContext.Roles.FirstAsync(r => r.Name == roleName, cancellationToken);
        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || user.Status == UserStatus.Disabled || user.Status == UserStatus.Suspended)
        {
            throw new AuthException("Invalid email or password.", 401);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new AuthException("Invalid email or password.", 401);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResultDto> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthException("Refresh token is required.", 401);
        }

        var tokenHash = HashToken(request.RefreshToken);
        var existingToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new AuthException("Refresh token is invalid or expired.", 401);
        }

        if (existingToken.IsRevoked || existingToken.IsUsed || existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            existingToken.IsRevoked = true;
            existingToken.RevokedAt ??= DateTime.UtcNow;
            existingToken.Reason = "Token reuse or expired token detected";
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException("Refresh token is invalid or expired.", 401);
        }

        var currentUser = existingToken.User;
        if (currentUser.Status == UserStatus.Disabled || currentUser.Status == UserStatus.Suspended)
        {
            throw new AuthException("Account is not active.", 401);
        }

        var replacementToken = CreateSecureToken();
        var replacementHash = HashToken(replacementToken);

        var activeCurrentToken = await _dbContext.RefreshTokens
            .Where(x => x.UserId == currentUser.Id && x.TokenHash == tokenHash && !x.IsRevoked && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .SingleOrDefaultAsync(cancellationToken);

        if (activeCurrentToken is null)
        {
            throw new AuthException("Refresh token is invalid or expired.", 401);
        }

        activeCurrentToken.IsUsed = true;
        activeCurrentToken.RevokedAt ??= DateTime.UtcNow;
        activeCurrentToken.Reason = "Token rotated";
        activeCurrentToken.ReplacedByTokenHash = replacementHash;

        var replacement = new RefreshToken
        {
            UserId = currentUser.Id,
            TokenHash = replacementHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedAt = DateTime.UtcNow,
            JwtId = Guid.NewGuid().ToString("N")
        };

        _dbContext.RefreshTokens.Add(replacement);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResultAsync(currentUser, cancellationToken, replacementToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthException("Refresh token is required.", 400);
        }

        var tokenHash = HashToken(request.RefreshToken);
        var activeToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (activeToken is null)
        {
            return;
        }

        activeToken.IsRevoked = true;
        activeToken.RevokedAt = DateTime.UtcNow;
        activeToken.Reason = "User logout";

        var user = await _dbContext.Users.Include(x => x.AdminSessions).FirstOrDefaultAsync(x => x.Id == activeToken.UserId, cancellationToken);
        if (user is not null)
        {
            foreach (var session in user.AdminSessions.Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow))
            {
                session.RevokedAt = DateTime.UtcNow;
                session.RevocationReason = "User logout";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OtpResultDto> RequestOtpAsync(OtpRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (!IsValidEmail(email))
        {
            throw new AuthException("A valid email is required.", 400);
        }

        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? "EmailVerification" : request.Purpose;
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        var priorOtp = await _dbContext.OtpVerifications
            .Where(x => x.Email == email && x.Purpose == purpose && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (priorOtp is not null && priorOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
        {
            return new OtpResultDto(false, "A code was recently sent. Please wait before requesting another.");
        }

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
        var hashedCode = HashToken(code, false);

        var otp = new OtpVerification
        {
            UserId = user?.Id,
            Email = email,
            Purpose = purpose,
            CodeHash = hashedCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            AttemptCount = 0,
            ResendCount = (priorOtp?.ResendCount ?? 0) + 1,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.OtpVerifications.Add(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _emailSender.SendOtpAsync(email, purpose, code, cancellationToken);

        return new OtpResultDto(true, "A verification code has been sent to your email.");
    }

    public async Task<OtpResultDto> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var purpose = request.Purpose;
        var code = request.Code.Trim();

        var otp = await _dbContext.OtpVerifications
            .Where(x => x.Email == email && x.Purpose == purpose && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            throw new AuthException("The verification code is invalid or has already been used.", 400);
        }

        if (otp.ExpiresAt <= DateTime.UtcNow)
        {
            otp.IsUsed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException("The verification code has expired.", 400);
        }

        otp.AttemptCount++;

        if (otp.AttemptCount >= 5)
        {
            otp.IsUsed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException("Too many failed attempts. Please request a new code.", 429);
        }

        if (HashToken(code, false) != otp.CodeHash)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthException("Invalid verification code.", 400);
        }

        otp.IsUsed = true;
        if (purpose == "EmailVerification" && otp.UserId.HasValue)
        {
            var user = await _dbContext.Users.FirstAsync(x => x.Id == otp.UserId.Value, cancellationToken);
            user.IsEmailVerified = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OtpResultDto(true, "Verification successful.");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new AuthException("New password must be at least 8 characters long.", 400);
        }

        var otpResult = await VerifyOtpAsync(new VerifyOtpRequest(request.Email, request.Purpose, request.Code), cancellationToken);
        if (!otpResult.Success)
        {
            throw new AuthException(otpResult.Message, 400);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user is null)
        {
            throw new AuthException("Account not found.", 404);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfileDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new AuthException("User not found.", 404);
        }

        var roles = user.UserRoles.Select(x => x.Role.Name).Distinct().ToArray();
        return new UserProfileDto(user.Id, user.FirstName, user.LastName, user.Email, user.IsEmailVerified, user.Status, roles);
    }

    public async Task<AuthResultDto> SignInWithGoogleAsync(GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Credential))
        {
            throw new AuthException("Google credential is required.", 400);
        }

        if (string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId) || _googleAuthOptions.ClientId.Contains("replace", StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthException("Google OAuth is not configured on this server.", 503);
        }

        var payload = await GoogleTokenValidator.ValidateAsync(request.Credential, _googleAuthOptions.ClientId);
        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new AuthException("Google sign-in failed: email was not verified.", 401);
        }

        var normalizedEmail = payload.Email.Trim();
        var user = await _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail || x.GoogleSubject == payload.Subject, cancellationToken);

        if (user is null)
        {
            await EnsureRolesExistAsync(cancellationToken);
            user = new User
            {
                FirstName = string.IsNullOrWhiteSpace(payload.GivenName) ? "Google" : payload.GivenName,
                LastName = string.IsNullOrWhiteSpace(payload.FamilyName) ? "User" : payload.FamilyName,
                Email = normalizedEmail,
                UserName = normalizedEmail,
                GoogleSubject = payload.Subject,
                IsEmailVerified = true,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var role = await _dbContext.Roles.FirstAsync(r => r.Name == "JobSeeker", cancellationToken);
            _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user.GoogleSubject ??= payload.Subject;
            user.IsEmailVerified = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public static string HashToken(string token, bool useSha256 = true)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be empty.", nameof(token));
        }

        if (!useSha256)
        {
            return token;
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }

    public static string CreateSecureToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(randomBytes).Replace("+", string.Empty).Replace("/", string.Empty).Replace("=", string.Empty);
    }

    private async Task<AuthResultDto> CreateAuthResultAsync(User user, CancellationToken cancellationToken, string? suppliedRefreshToken = null)
    {
        await EnsureRolesExistAsync(cancellationToken);

        var refreshToken = suppliedRefreshToken ?? CreateSecureToken();
        var tokenHash = HashToken(refreshToken);

        var userRoles = await _dbContext.UserRoles
            .Include(x => x.Role)
            .Where(x => x.UserId == user.Id)
            .Select(x => x.Role.Name)
            .ToListAsync(cancellationToken);

        var adminSessionId = await CreateOrRotateAdminSessionAsync(user, cancellationToken, userRoles);
        var accessToken = GenerateAccessToken(user, adminSessionId, userRoles);

        var refreshRecord = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedAt = DateTime.UtcNow,
            JwtId = Guid.NewGuid().ToString("N")
        };

        _dbContext.RefreshTokens.Add(refreshRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            user.Id,
            user.Email,
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes),
            userRoles);
    }

    private async Task<string?> CreateOrRotateAdminSessionAsync(User user, CancellationToken cancellationToken, ICollection<string>? roles = null)
    {
        var roleList = roles ?? (await _dbContext.UserRoles.Include(x => x.Role).Where(x => x.UserId == user.Id).Select(x => x.Role.Name).ToListAsync(cancellationToken));
        if (!roleList.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var currentSessions = await _dbContext.AdminSessions
            .Where(x => x.UserId == user.Id && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var session in currentSessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "Replaced by new admin login";
        }

        var newSession = new AdminSession
        {
            UserId = user.Id,
            SessionIdentifier = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            CreatedAt = DateTime.UtcNow,
            IpAddress = "server-side"
        };

        _dbContext.AdminSessions.Add(newSession);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return newSession.SessionIdentifier;
    }

    private string GenerateAccessToken(User user, string? adminSessionId, ICollection<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (!string.IsNullOrWhiteSpace(adminSessionId))
        {
            claims.Add(new Claim("sid", adminSessionId));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.Split('@').Length == 2;
    }

    private async Task EnsureRolesExistAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Roles.Select(x => x.Name).ToListAsync(cancellationToken);
        var required = new[] { "JobSeeker", "Employer", "Admin" };
        foreach (var roleName in required)
        {
            if (existing.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            _dbContext.Roles.Add(new Role
            {
                Name = roleName,
                Description = $"{roleName} role",
                RoleType = roleName switch
                {
                    "JobSeeker" => UserRoleType.JobSeeker,
                    "Employer" => UserRoleType.Employer,
                    "Admin" => UserRoleType.Admin,
                    _ => UserRoleType.JobSeeker
                }
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeRole(string roleName)
    {
        var normalized = (roleName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "JobSeeker";
        }

        return normalized.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : normalized.Equals("employer", StringComparison.OrdinalIgnoreCase) ? "Employer" : "JobSeeker";
    }
}
