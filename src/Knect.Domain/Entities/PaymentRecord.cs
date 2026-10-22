using Knect.Domain.Common;
using Knect.Domain.Enums;

namespace Knect.Domain.Entities;

public class PaymentRecord : BaseEntity
{
    public Guid EmployerProfileId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public string? EvidenceFileKey { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ReviewNotes { get; set; }

    public EmployerProfile EmployerProfile { get; set; } = null!;
    public Subscription? Subscription { get; set; }
    public User? ReviewedByAdmin { get; set; }
}
