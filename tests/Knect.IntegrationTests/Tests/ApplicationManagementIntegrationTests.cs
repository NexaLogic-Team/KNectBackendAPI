using Knect.Application.Applications.DTOs;
using Knect.Application.Applications.Interfaces;
using Knect.Application.Authentication.Exceptions;
using Knect.Application.FileStorage;
using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Applications;
using Knect.Infrastructure.FileStorage;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knect.IntegrationTests.Tests;

public class ApplicationManagementIntegrationTests
{
    private static string? TestConnectionString =>
        Environment.GetEnvironmentVariable("KNECT_TEST_CONNECTION_STRING") ??
        Environment.GetEnvironmentVariable("ConnectionStrings__TestConnection");

    private static bool IsConfigured() => !string.IsNullOrWhiteSpace(TestConnectionString);

    private static ServiceProvider CreateServiceProvider()
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException("PostgreSQL application tests are not configured. Set KNECT_TEST_CONNECTION_STRING or ConnectionStrings__TestConnection.");
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<KnectDbContext>(options => options.UseNpgsql(TestConnectionString!));
        services.AddSingleton<IFileStorageService, MemoryFileStorageService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CanCreateApplication_WhenJobIsPublishedAndEligible()
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

            var applicant = new User
            {
                FirstName = "Applicant",
                LastName = "User",
                Email = $"{Guid.NewGuid():N}@example.com",
                UserName = "applicant",
                PasswordHash = "hash",
                Status = UserStatus.Active,
                IsEmailVerified = true,
                CvFileKey = "users/applicant/cv.pdf",
                CreatedAt = DateTime.UtcNow
            };

            var employer = new User
            {
                FirstName = "Employer",
                LastName = "User",
                Email = $"{Guid.NewGuid():N}@example.com",
                UserName = "employer",
                PasswordHash = "hash",
                Status = UserStatus.Active,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            var employerProfile = new EmployerProfile
            {
                User = employer,
                CompanyName = "Example Employer",
                CreatedAt = DateTime.UtcNow
            };

            var category = new JobCategory { Name = "Engineering", IsActive = true, CreatedAt = DateTime.UtcNow };
            var experience = new JobExperience { Name = "Senior", IsActive = true, CreatedAt = DateTime.UtcNow };
            var job = new Job
            {
                EmployerProfile = employerProfile,
                Category = category,
                Experience = experience,
                Title = "Senior C# Developer",
                Slug = "senior-csharp-developer",
                Description = "Build backend systems.",
                Location = "Remote",
                EmploymentType = "Full-time",
                Currency = "USD",
                Status = JobStatus.Published,
                PublishedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            db.Users.AddRange(applicant, employer);
            db.JobCategories.Add(category);
            db.JobExperiences.Add(experience);
            db.EmployerProfiles.Add(employerProfile);
            db.Jobs.Add(job);
            await db.SaveChangesAsync();

            var appService = scope.ServiceProvider.GetRequiredService<IApplicationService>();
            var created = await appService.ApplyAsync(applicant.Id, job.Id, new ApplyToJobRequest("Hello world", applicant.CvFileKey, "cv.pdf"), CancellationToken.None);

            Assert.Equal(job.Id, created.JobId);
            Assert.Equal(ApplicationStatus.Submitted, created.Status);
        }
    }

    [Fact]
    public async Task RejectsDuplicateApplication_WhenAlreadySubmitted()
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

            var applicant = new User
            {
                FirstName = "Duplicate",
                LastName = "User",
                Email = $"{Guid.NewGuid():N}@example.com",
                UserName = "duplicate-user",
                PasswordHash = "hash",
                Status = UserStatus.Active,
                IsEmailVerified = true,
                CvFileKey = "users/duplicate/cv.pdf",
                CreatedAt = DateTime.UtcNow
            };

            var employer = new User
            {
                FirstName = "Duplicate",
                LastName = "Employer",
                Email = $"{Guid.NewGuid():N}@example.com",
                UserName = "duplicate-employer",
                PasswordHash = "hash",
                Status = UserStatus.Active,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            var employerProfile = new EmployerProfile
            {
                User = employer,
                CompanyName = "Duplicate Company",
                CreatedAt = DateTime.UtcNow
            };

            var category = new JobCategory { Name = "Support", IsActive = true, CreatedAt = DateTime.UtcNow };
            var experience = new JobExperience { Name = "Mid", IsActive = true, CreatedAt = DateTime.UtcNow };
            var job = new Job
            {
                EmployerProfile = employerProfile,
                Category = category,
                Experience = experience,
                Title = "Support Engineer",
                Slug = "support-engineer",
                Description = "Support customers.",
                Location = "Remote",
                EmploymentType = "Full-time",
                Currency = "USD",
                Status = JobStatus.Published,
                PublishedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            db.Users.AddRange(applicant, employer);
            db.JobCategories.Add(category);
            db.JobExperiences.Add(experience);
            db.EmployerProfiles.Add(employerProfile);
            db.Jobs.Add(job);
            await db.SaveChangesAsync();

            var appService = scope.ServiceProvider.GetRequiredService<IApplicationService>();
            await appService.ApplyAsync(applicant.Id, job.Id, new ApplyToJobRequest("First submit", applicant.CvFileKey, "cv.pdf"), CancellationToken.None);

            await Assert.ThrowsAsync<AuthException>(() => appService.ApplyAsync(applicant.Id, job.Id, new ApplyToJobRequest("Second submit", applicant.CvFileKey, "cv.pdf"), CancellationToken.None));
        }
    }
}
