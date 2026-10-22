using Knect.Domain.Entities;
using Knect.Domain.Enums;

namespace Knect.Infrastructure.BackgroundJobs;

public static class SubscriptionPublishingPolicy
{
    public static bool IsSubscriptionEligible(Subscription? subscription, DateTime utcNow)
        => subscription is { Status: SubscriptionStatus.Active } && subscription.EndDate > utcNow;

    public static bool ShouldExpire(Subscription? subscription, DateTime utcNow)
        => subscription is { Status: SubscriptionStatus.Active } && subscription.EndDate <= utcNow;

    public static bool CanPublish(int activePublishedJobs, int maxJobPosts)
        => maxJobPosts >= 0 && activePublishedJobs < maxJobPosts;

    public static bool IsEligibleForPublishing(Subscription? subscription, int activePublishedJobs, int maxJobPosts, DateTime utcNow)
        => IsSubscriptionEligible(subscription, utcNow) && CanPublish(activePublishedJobs, maxJobPosts);
}
