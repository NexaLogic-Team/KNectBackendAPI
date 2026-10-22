using Knect.Domain.Entities;
using Knect.Domain.Enums;

namespace Knect.UnitTests.Tests;

public class DomainRuleTests
{
    [Fact]
    public void JobTransition_AllowsDraftToPublished()
    {
        var allowed = Job.IsTransitionAllowed(JobStatus.Draft, JobStatus.Published);
        Assert.True(allowed);
    }

    [Fact]
    public void JobTransition_DoesNotAllowClosedToPublished()
    {
        var allowed = Job.IsTransitionAllowed(JobStatus.Closed, JobStatus.Published);
        Assert.False(allowed);
    }

    [Fact]
    public void ApplicationTransition_AllowsReviewToShortlist()
    {
        var allowed = JobApplication.IsTransitionAllowed(ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted);
        Assert.True(allowed);
    }

    [Fact]
    public void ApplicationTransition_BlockInvalidTransition()
    {
        var allowed = JobApplication.IsTransitionAllowed(ApplicationStatus.Accepted, ApplicationStatus.Submitted);
        Assert.False(allowed);
    }
}
