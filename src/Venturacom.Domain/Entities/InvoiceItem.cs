using Venturacom.Domain.Common;

namespace Venturacom.Domain.Entities;

public sealed class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = default!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineSubTotal { get; set; }
    public decimal LineTax { get; set; }
    public decimal LineTotal { get; set; }
}
