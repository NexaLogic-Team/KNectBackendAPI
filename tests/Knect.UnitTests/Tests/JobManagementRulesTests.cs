using Knect.Domain.Entities;
using Knect.Domain.Enums;

namespace Knect.UnitTests.Tests;

public class JobManagementRulesTests
{
    [Fact]
    public void JobTransition_AllowsDraftToPublishedAndPaused()
    {
        Assert.True(Job.IsTransitionAllowed(JobStatus.Draft, JobStatus.Published));
        Assert.True(Job.IsTransitionAllowed(JobStatus.Draft, JobStatus.Paused));
    }

    [Fact]
    public void JobTransition_RejectsInvalidSequence()
    {
        Assert.False(Job.IsTransitionAllowed(JobStatus.Closed, JobStatus.Published));
        Assert.False(Job.IsTransitionAllowed(JobStatus.Published, JobStatus.Draft));
        Assert.False(Job.IsTransitionAllowed(JobStatus.Archived, JobStatus.Published));
    }

    [Fact]
    public void JobStatus_DefinesExpectedWorkflow()
    {
        Assert.Equal(5, Enum.GetValues<JobStatus>().Length);
        Assert.Contains(JobStatus.Published, Enum.GetValues<JobStatus>());
        Assert.Contains(JobStatus.Archived, Enum.GetValues<JobStatus>());
    }
}
