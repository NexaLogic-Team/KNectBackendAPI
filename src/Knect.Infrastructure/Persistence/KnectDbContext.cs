using Knect.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Knect.Infrastructure.Persistence;

public class KnectDbContext : DbContext
{
    public KnectDbContext(DbContextOptions<KnectDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmployerProfile> EmployerProfiles => Set<EmployerProfile>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<JobCategory> JobCategories => Set<JobCategory>();
    public DbSet<JobExperience> JobExperiences => Set<JobExperience>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.GoogleSubject).IsUnique();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.UserName).HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.PhoneNumber).HasMaxLength(30);
            entity.Property(x => x.Location).HasMaxLength(200);
            entity.Property(x => x.ProfessionalSummary).HasMaxLength(2000);
            entity.Property(x => x.Skills).HasMaxLength(2000);
            entity.Property(x => x.WorkExperience).HasMaxLength(4000);
            entity.Property(x => x.Education).HasMaxLength(4000);
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(1024);
            entity.Property(x => x.CvFileKey).HasMaxLength(512);
            entity.Property(x => x.CvFileName).HasMaxLength(255);
            entity.Property(x => x.GoogleSubject).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.LastLoginAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.Property(x => x.TokenHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.JwtId).HasMaxLength(100);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(512);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.RevokedAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.ToTable("OtpVerifications");
            entity.HasIndex(x => new { x.Email, x.Purpose, x.IsUsed });
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Purpose).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CodeHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.User).WithMany(x => x.Otps).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AdminSession>(entity =>
        {
            entity.ToTable("AdminSessions");
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.SessionIdentifier).IsUnique();
            entity.Property(x => x.SessionIdentifier).HasMaxLength(128).IsRequired();
            entity.Property(x => x.DeviceInfo).HasMaxLength(500);
            entity.Property(x => x.IpAddress).HasMaxLength(45);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.RevokedAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.User).WithMany(x => x.AdminSessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(x => x.EventType);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.DetailsJson).HasColumnType("jsonb");
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmployerProfile>(entity =>
        {
            entity.ToTable("EmployerProfiles");
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.CompanyName);
            entity.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.CompanyDescription).HasMaxLength(2000);
            entity.Property(x => x.Industry).HasMaxLength(100);
            entity.Property(x => x.CompanySize).HasMaxLength(50);
            entity.Property(x => x.Website).HasMaxLength(255);
            entity.Property(x => x.ContactEmail).HasMaxLength(255);
            entity.Property(x => x.ContactPhone).HasMaxLength(30);
            entity.Property(x => x.LogoUrl).HasMaxLength(1024);
            entity.HasOne(x => x.User).WithOne(x => x.EmployerProfile).HasForeignKey<EmployerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("SubscriptionPlans");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Price).HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.EmployerProfileId);
            entity.HasIndex(x => x.PlanId);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.StartDate).HasColumnType("timestamp with time zone");
            entity.Property(x => x.EndDate).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.EmployerProfile).WithMany(x => x.Subscriptions).HasForeignKey(x => x.EmployerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Plan).WithMany(x => x.Subscriptions).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany(x => x.Subscriptions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("PaymentRecords");
            entity.HasIndex(x => x.EmployerProfileId);
            entity.HasIndex(x => x.SubscriptionId);
            entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            entity.Property(x => x.PaymentMethod).HasMaxLength(60).IsRequired();
            entity.Property(x => x.PaymentReference).HasMaxLength(255).IsRequired();
            entity.Property(x => x.EvidenceFileKey).HasMaxLength(512);
            entity.Property(x => x.ReviewNotes).HasMaxLength(1000);
            entity.Property(x => x.SubmittedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.EmployerProfile).WithMany(x => x.Payments).HasForeignKey(x => x.EmployerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Subscription).WithMany(x => x.Payments).HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ReviewedByAdmin).WithMany().HasForeignKey(x => x.ReviewedByAdminId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<JobCategory>(entity =>
        {
            entity.ToTable("JobCategories");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<JobExperience>(entity =>
        {
            entity.ToTable("JobExperiences");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("Jobs");
            entity.HasIndex(x => x.EmployerProfileId);
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.PublishedAt);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Description).HasColumnType("text").IsRequired();
            entity.Property(x => x.Responsibilities).HasColumnType("text");
            entity.Property(x => x.Requirements).HasColumnType("text");
            entity.Property(x => x.Location).HasMaxLength(200);
            entity.Property(x => x.EmploymentType).HasMaxLength(60);
            entity.Property(x => x.Currency).HasMaxLength(10);
            entity.Property(x => x.SalaryMin).HasColumnType("numeric(18,2)");
            entity.Property(x => x.SalaryMax).HasColumnType("numeric(18,2)");
            entity.Property(x => x.PublishedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.EmployerProfile).WithMany(x => x.Jobs).HasForeignKey(x => x.EmployerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany(x => x.Jobs).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Experience).WithMany(x => x.Jobs).HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.ToTable("JobApplications");
            entity.HasIndex(x => new { x.JobId, x.UserId }).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.CoverLetter).HasColumnType("text");
            entity.Property(x => x.CvFileKey).HasMaxLength(512);
            entity.Property(x => x.AppliedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.HasOne(x => x.Job).WithMany(x => x.Applications).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany(x => x.JobApplications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
