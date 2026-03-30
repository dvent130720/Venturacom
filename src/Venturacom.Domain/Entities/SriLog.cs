using Venturacom.Domain.Common;

namespace Venturacom.Domain.Entities;

public sealed class SriLog : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = default!;
    public string Direction { get; set; } = string.Empty;
    public string XmlContent { get; set; } = string.Empty;
    public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;
}
