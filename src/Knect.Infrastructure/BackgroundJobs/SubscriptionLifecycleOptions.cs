namespace Knect.Infrastructure.BackgroundJobs;

public sealed class SubscriptionLifecycleOptions
{
    public const string SectionName = "BackgroundJobs:SubscriptionLifecycle";

    public bool Enabled { get; set; } = true;
    public int CheckIntervalMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 200;
    public int ExpiryReminderDays { get; set; } = 7;
    public int FinalReminderDays { get; set; } = 1;
    public int MaxRetryAttempts { get; set; } = 3;
}
