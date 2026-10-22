using Knect.Domain.Common;

namespace Knect.Domain.Entities;

public class OtpVerification : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int AttemptCount { get; set; }
    public int ResendCount { get; set; }

    public User? User { get; set; }
}
