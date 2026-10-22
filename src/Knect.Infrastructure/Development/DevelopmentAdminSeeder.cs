using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Knect.Infrastructure.Development;

public sealed class DevelopmentAdminSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<DevelopmentAdminSeedOptions> _options;
    private readonly ILogger<DevelopmentAdminSeeder> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public DevelopmentAdminSeeder(
        IServiceProvider serviceProvider,
        IOptions<DevelopmentAdminSeedOptions> options,
        ILogger<DevelopmentAdminSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is not null &&
            !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var email = _options.Value.Email?.Trim();
        var password = _options.Value.Password;

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            _logger.LogWarning("Development admin seeding skipped: DevelopmentAdmin:Email and DevelopmentAdmin:Password must be configured in development mode.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnectDbContext>();

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role
            {
                Name = "Admin",
                Description = "Platform administrator",
                RoleType = UserRoleType.Admin,
                CreatedAt = DateTime.UtcNow
            };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var existingUser = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser is null)
        {
            var seedPasswordUser = new User
            {
                FirstName = "System",
                LastName = "Admin",
                Email = email,
                UserName = email,
                Status = UserStatus.Active,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            var user = new User
            {
                FirstName = "System",
                LastName = "Admin",
                Email = email,
                UserName = email,
                PasswordHash = _passwordHasher.HashPassword(seedPasswordUser, password),
                Status = UserStatus.Active,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Development admin account seeded successfully.");
            return;
        }

        var hasAdminRole = existingUser.UserRoles.Any(ur => ur.RoleId == adminRole.Id || ur.Role.Name == "Admin");
        if (!hasAdminRole)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = existingUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Development admin role attached to existing user.");
        }

        if (string.IsNullOrWhiteSpace(existingUser.PasswordHash))
        {
            existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, password);
            existingUser.IsEmailVerified = true;
            existingUser.Status = UserStatus.Active;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Development admin password initialized for existing user.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
