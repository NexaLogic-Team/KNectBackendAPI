using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.BackgroundJobs;

namespace Knect.UnitTests.Tests;

public class SubscriptionLifecyclePolicyTests
{
    [Fact]
    public void SubscriptionPublishingPolicy_RejectsExpiredSubscriptions()
    {
        var now = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            EndDate = now.AddDays(-1)
        };

        Assert.False(SubscriptionPublishingPolicy.IsSubscriptionEligible(subscription, now));
        Assert.True(SubscriptionPublishingPolicy.ShouldExpire(subscription, now));
    }

    [Fact]
    public void SubscriptionPublishingPolicy_AllowsActiveSubscriptionsWithinLimit()
    {
        var now = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            EndDate = now.AddDays(7)
        };

        Assert.True(SubscriptionPublishingPolicy.IsSubscriptionEligible(subscription, now));
        Assert.True(SubscriptionPublishingPolicy.IsEligibleForPublishing(subscription, 2, 5, now));
    }

    [Fact]
    public void SubscriptionPublishingPolicy_RejectsWhenLimitReached()
    {
        var now = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            EndDate = now.AddDays(7)
        };

        Assert.False(SubscriptionPublishingPolicy.IsEligibleForPublishing(subscription, 5, 5, now));
        Assert.False(SubscriptionPublishingPolicy.CanPublish(5, 5));
    }
}
