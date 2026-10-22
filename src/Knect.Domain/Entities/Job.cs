using Knect.Domain.Common;
using Knect.Domain.Enums;

namespace Knect.Domain.Entities;

public class Job : BaseEntity
{
    public Guid EmployerProfileId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid ExperienceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Responsibilities { get; set; }
    public string? Requirements { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public bool IsDeleted { get; set; }

    public EmployerProfile EmployerProfile { get; set; } = null!;
    public JobCategory Category { get; set; } = null!;
    public JobExperience Experience { get; set; } = null!;
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

    public static bool IsTransitionAllowed(JobStatus current, JobStatus target)
        => (current, target) switch
        {
            (JobStatus.Draft, JobStatus.Published) => true,
            (JobStatus.Draft, JobStatus.Paused) => true,
            (JobStatus.Published, JobStatus.Paused) => true,
            (JobStatus.Published, JobStatus.Closed) => true,
            (JobStatus.Paused, JobStatus.Published) => true,
            (JobStatus.Paused, JobStatus.Closed) => true,
            _ => false
        };
}
