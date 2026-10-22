using Knect.Domain.Common;
using Knect.Domain.Enums;

namespace Knect.Domain.Entities;

public class JobApplication : BaseEntity
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public string? CoverLetter { get; set; }
    public string? CvFileKey { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Job Job { get; set; } = null!;
    public User User { get; set; } = null!;

    public static bool IsTransitionAllowed(ApplicationStatus current, ApplicationStatus target)
        => (current, target) switch
        {
            (ApplicationStatus.Submitted, ApplicationStatus.UnderReview) => true,
            (ApplicationStatus.Submitted, ApplicationStatus.Rejected) => true,
            (ApplicationStatus.Submitted, ApplicationStatus.Cancelled) => true,
            (ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted) => true,
            (ApplicationStatus.UnderReview, ApplicationStatus.Rejected) => true,
            (ApplicationStatus.UnderReview, ApplicationStatus.Accepted) => true,
            (ApplicationStatus.Shortlisted, ApplicationStatus.Accepted) => true,
            (ApplicationStatus.Shortlisted, ApplicationStatus.Rejected) => true,
            _ => false
        };
}
