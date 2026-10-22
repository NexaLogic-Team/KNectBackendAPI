using Knect.Domain.Common;
using Knect.Domain.Enums;

namespace Knect.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid EmployerProfileId { get; set; }
    public Guid PlanId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public bool IsAutoRenew { get; set; }
    public string? Notes { get; set; }

    public EmployerProfile EmployerProfile { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<PaymentRecord> Payments { get; set; } = new List<PaymentRecord>();
}
