using Knect.Application.Authentication;
using Knect.Application.Authentication.DTOs;
using Knect.Application.Authentication.Exceptions;
using Knect.Application.Authentication.Interfaces;
using Knect.Infrastructure.Authentication;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knect.IntegrationTests.Tests;

public class PostgresAuthIntegrationTests
{
    private static string? TestConnectionString =>
        Environment.GetEnvironmentVariable("KNECT_TEST_CONNECTION_STRING") ??
        Environment.GetEnvironmentVariable("ConnectionStrings__TestConnection");

    private static bool IsConfigured() => !string.IsNullOrWhiteSpace(TestConnectionString);

    private static ServiceProvider CreateServiceProvider()
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException("PostgreSQL integration tests are not configured. Set KNECT_TEST_CONNECTION_STRING or ConnectionStrings__TestConnection.");
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = "https://api.test.local";
            options.Audience = "knect-test";
            options.Key = "ThisIsA32ByteTestSigningKey1234567890";
            options.AccessTokenLifetimeMinutes = 15;
            options.RefreshTokenLifetimeDays = 14;
        });
        services.Configure<GoogleAuthOptions>(options => options.ClientId = "test-client-id");
        services.AddDbContext<KnectDbContext>(options => options.UseNpgsql(TestConnectionString!));
        services.AddScoped<IEmailSender, TestEmailSender>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IGoogleAuthenticationService, AuthenticationService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RegisterLoginRefreshLogout_UsesPostgresStoreAcrossScopes()
    {
        if (!IsConfigured())
        {
            return;
        }

        using var provider = CreateServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnectDbContext>();
            await db.Database.MigrateAsync();
        }

        var email = $"{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        using (var outer = provider.CreateScope())
        {
            var auth = outer.ServiceProvider.GetRequiredService<IAuthenticationService>();
            var registered = await auth.RegisterAsync(new RegisterRequest("Test", "User", email, password, "JobSeeker"), CancellationToken.None);
            Assert.Equal(email, registered.Email);
        }

        using (var inner = provider.CreateScope())
        {
            var auth = inner.ServiceProvider.GetRequiredService<IAuthenticationService>();
            var loggedIn = await auth.LoginAsync(new LoginRequest(email, password), CancellationToken.None);
            Assert.NotNull(loggedIn.AccessToken);

            var refreshed = await auth.RefreshAsync(new RefreshRequest(loggedIn.RefreshToken), CancellationToken.None);
            Assert.NotEqual(loggedIn.RefreshToken, refreshed.RefreshToken);

            await auth.LogoutAsync(new LogoutRequest(refreshed.RefreshToken), CancellationToken.None);

            var db = inner.ServiceProvider.GetRequiredService<KnectDbContext>();
            var tokens = await db.RefreshTokens.Where(x => x.UserId == db.Users.Single(x => x.Email == email).Id).ToListAsync();
            Assert.NotEmpty(tokens);
            Assert.Contains(tokens, x => x.IsRevoked || x.IsUsed);
        }
    }

    [Fact]
    public async Task OtpVerificationResendAndReplay_AreEnforcedInPostgres()
    {
        if (!IsConfigured())
        {
            return;
        }

        using var provider = CreateServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnectDbContext>();
            await db.Database.MigrateAsync();
        }

        var email = $"{Guid.NewGuid():N}@example.com";
        using (var scope = provider.CreateScope())
        {
            var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            await auth.RegisterAsync(new RegisterRequest("OTP", "User", email, "Password123!", "JobSeeker"), CancellationToken.None);

            var firstRequest = await auth.RequestOtpAsync(new OtpRequest(email, "EmailVerification"), CancellationToken.None);
            Assert.True(firstRequest.Success);

            var db = scope.ServiceProvider.GetRequiredService<KnectDbContext>();
            var record = await db.OtpVerifications
                .Where(x => x.Email == email && x.Purpose == "EmailVerification")
                .OrderByDescending(x => x.CreatedAt)
                .FirstAsync();
            var code = record.CodeHash;

            var verify = await auth.VerifyOtpAsync(new VerifyOtpRequest(email, "EmailVerification", code), CancellationToken.None);
            Assert.True(verify.Success);

            await Assert.ThrowsAsync<AuthException>(() => auth.VerifyOtpAsync(new VerifyOtpRequest(email, "EmailVerification", code), CancellationToken.None));

            var repeatedRequest = await auth.RequestOtpAsync(new OtpRequest(email, "EmailVerification"), CancellationToken.None);
            Assert.False(repeatedRequest.Success);

            var wrongAttempt = await Assert.ThrowsAsync<AuthException>(() => auth.VerifyOtpAsync(new VerifyOtpRequest(email, "EmailVerification", "000000"), CancellationToken.None));
            Assert.Equal(400, wrongAttempt.StatusCode);
        }
    }

    [Fact]
    public async Task PasswordResetUsesOtpOnceAndExpiresAfterAttempt()
    {
        if (!IsConfigured())
        {
            return;
        }

        using var provider = CreateServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnectDbContext>();
            await db.Database.MigrateAsync();
        }

        var email = $"{Guid.NewGuid():N}@example.com";
        const string initialPassword = "Password123!";
        const string newPassword = "Password456!";

        using (var scope = provider.CreateScope())
        {
            var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            await auth.RegisterAsync(new RegisterRequest("Reset", "User", email, initialPassword, "JobSeeker"), CancellationToken.None);

            var request = await auth.RequestOtpAsync(new OtpRequest(email, "PasswordReset"), CancellationToken.None);
            Assert.True(request.Success);

            var db = scope.ServiceProvider.GetRequiredService<KnectDbContext>();
            var otp = await db.OtpVerifications
                .Where(x => x.Email == email && x.Purpose == "PasswordReset")
                .OrderByDescending(x => x.CreatedAt)
                .FirstAsync();

            var code = otp.CodeHash;
            await auth.ResetPasswordAsync(new ResetPasswordRequest(email, "PasswordReset", code, newPassword), CancellationToken.None);

            var loginWithOldPassword = await Assert.ThrowsAsync<AuthException>(() => auth.LoginAsync(new LoginRequest(email, initialPassword), CancellationToken.None));
            Assert.Equal(401, loginWithOldPassword.StatusCode);

            var loggedIn = await auth.LoginAsync(new LoginRequest(email, newPassword), CancellationToken.None);
            Assert.NotNull(loggedIn.AccessToken);

            await Assert.ThrowsAsync<AuthException>(() => auth.ResetPasswordAsync(new ResetPasswordRequest(email, "PasswordReset", code, "Password789!"), CancellationToken.None));
        }
    }

    private sealed class TestEmailSender : IEmailSender
    {
        public Task SendOtpAsync(string email, string purpose, string code, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
