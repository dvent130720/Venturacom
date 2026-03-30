using Venturacom.Domain.Common;

namespace Venturacom.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}
