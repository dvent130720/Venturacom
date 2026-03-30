using Venturacom.Domain.Common;
using Venturacom.Domain.Enums;

namespace Venturacom.Domain.Entities;

public sealed class InvoiceJob : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = default!;
    public InvoiceJobStatus Status { get; set; } = InvoiceJobStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime ScheduledAtUtc { get; set; } = DateTime.UtcNow;
    public string? LastError { get; set; }
}
