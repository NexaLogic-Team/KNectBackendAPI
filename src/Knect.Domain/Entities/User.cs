using Knect.Domain.Common;
using Knect.Domain.Enums;

namespace Knect.Domain.Entities;

public class User : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? UserName { get; set; }
    public string? PasswordHash { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Location { get; set; }
    public string? ProfessionalSummary { get; set; }
    public string? Skills { get; set; }
    public string? WorkExperience { get; set; }
    public string? Education { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? CvFileKey { get; set; }
    public string? CvFileName { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? GoogleSubject { get; set; }

    public EmployerProfile? EmployerProfile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<OtpVerification> Otps { get; set; } = new List<OtpVerification>();
    public ICollection<AdminSession> AdminSessions { get; set; } = new List<AdminSession>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
