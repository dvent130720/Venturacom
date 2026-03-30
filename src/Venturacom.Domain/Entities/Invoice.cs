using Venturacom.Domain.Common;
using Venturacom.Domain.Enums;

namespace Venturacom.Domain.Entities;

public sealed class Invoice : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<InvoiceJob> Jobs { get; set; } = new List<InvoiceJob>();
}
