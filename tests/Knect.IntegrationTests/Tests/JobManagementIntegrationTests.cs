using Knect.Application.Authentication;
using Knect.Application.Authentication.Interfaces;
using Knect.Application.Jobs.DTOs;
using Knect.Application.Jobs.Interfaces;
using Knect.Infrastructure.Authentication;
using Knect.Infrastructure.Jobs;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knect.IntegrationTests.Tests;

public class JobManagementIntegrationTests
{
    private static string? TestConnectionString =>
        Environment.GetEnvironmentVariable("KNECT_TEST_CONNECTION_STRING") ??
        Environment.GetEnvironmentVariable("ConnectionStrings__TestConnection");

    private static bool IsConfigured() => !string.IsNullOrWhiteSpace(TestConnectionString);

    private static ServiceProvider CreateServiceProvider()
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException("PostgreSQL job tests are not configured. Set KNECT_TEST_CONNECTION_STRING or ConnectionStrings__TestConnection.");
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
        services.AddScoped<IJobCategoryService, JobCategoryService>();
        services.AddScoped<IJobExperienceService, JobExperienceService>();
        services.AddScoped<IJobService, JobService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CanCreateEmployerCategoryAndJob_WhenDatabaseIsConfigured()
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

        var userId = Guid.NewGuid();
        var employerProfileId = Guid.NewGuid();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnectDbContext>();
            var user = new Knect.Domain.Entities.User
            {
                Id = userId,
                FirstName = "Employer",
                LastName = "User",
                Email = $"{Guid.NewGuid():N}@example.com",
                UserName = "employer",
                PasswordHash = "x",
                Status = Knect.Domain.Enums.UserStatus.Active,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);

            var profile = new Knect.Domain.Entities.EmployerProfile
            {
                Id = employerProfileId,
                UserId = user.Id,
                CompanyName = "Test Company",
                CreatedAt = DateTime.UtcNow
            };
            db.EmployerProfiles.Add(profile);
            await db.SaveChangesAsync();

            var categoryService = scope.ServiceProvider.GetRequiredService<IJobCategoryService>();
            var category = await categoryService.CreateAsync("Engineering", "Engineering roles", CancellationToken.None);
            Assert.NotEqual(Guid.Empty, category.Id);

            var experienceService = scope.ServiceProvider.GetRequiredService<IJobExperienceService>();
            var experience = await experienceService.CreateAsync("Mid-Level", "2-5 years", CancellationToken.None);
            Assert.NotEqual(Guid.Empty, experience.Id);

            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
            var created = await jobService.CreateAsync(user.Id, new CreateJobRequest(
                "Senior Backend Engineer",
                "Build APIs for Knect",
                category.Id,
                experience.Id,
                "Remote",
                "Full-time",
                120000m,
                160000m,
                "USD",
                "Own API design.",
                "C# and PostgreSQL.",
                DateTime.UtcNow.AddDays(30),
                Knect.Domain.Enums.JobStatus.Draft), CancellationToken.None);

            Assert.Equal("Senior Backend Engineer", created.Title);
            Assert.Equal(Knect.Domain.Enums.JobStatus.Draft, created.Status);
        }
    }

    [Fact]
    public async Task PublicSearchReturnsOnlyPublishedJobs_WhenDatabaseIsConfigured()
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

            var category = new Knect.Domain.Entities.JobCategory { Name = "Marketing", Description = "Marketing roles", IsActive = true, CreatedAt = DateTime.UtcNow };
            var experience = new Knect.Domain.Entities.JobExperience { Name = "Associate", Description = "Associate level", IsActive = true, CreatedAt = DateTime.UtcNow };
            var company = new Knect.Domain.Entities.EmployerProfile
            {
                CompanyName = "Public Company",
                User = new Knect.Domain.Entities.User
                {
                    FirstName = "Public",
                    LastName = "User",
                    Email = $"{Guid.NewGuid():N}@example.com",
                    UserName = "public-user",
                    PasswordHash = "x",
                    Status = Knect.Domain.Enums.UserStatus.Active,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                },
                CreatedAt = DateTime.UtcNow
            };

            db.JobCategories.Add(category);
            db.JobExperiences.Add(experience);
            db.EmployerProfiles.Add(company);
            await db.SaveChangesAsync();

            var published = new Knect.Domain.Entities.Job
            {
                EmployerProfileId = company.Id,
                CategoryId = category.Id,
                ExperienceId = experience.Id,
                Title = "Published Role",
                Slug = "published-role",
                Description = "A published role",
                Location = "Berlin",
                EmploymentType = "Remote",
                Currency = "USD",
                Status = Knect.Domain.Enums.JobStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var draft = new Knect.Domain.Entities.Job
            {
                EmployerProfileId = company.Id,
                CategoryId = category.Id,
                ExperienceId = experience.Id,
                Title = "Draft Role",
                Slug = "draft-role",
                Description = "A draft role",
                Location = "Berlin",
                EmploymentType = "Remote",
                Currency = "USD",
                Status = Knect.Domain.Enums.JobStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            db.Jobs.AddRange(published, draft);
            await db.SaveChangesAsync();

            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
            var result = await jobService.SearchPublicAsync(new JobSearchRequest(null, null, null, "Berlin", "Remote", null, null, "newest", 1, 10), CancellationToken.None);
            Assert.Single(result.Items);
            Assert.Equal("Published Role", result.Items[0].Title);
        }
    }
}
