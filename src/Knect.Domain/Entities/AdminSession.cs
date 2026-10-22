using Knect.Domain.Common;

namespace Knect.Domain.Entities;

public class AdminSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string SessionIdentifier { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public bool IsRevoked => RevokedAt is not null;

    public User User { get; set; } = null!;
}
