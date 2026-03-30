using Venturacom.Domain.Common;

namespace Venturacom.Domain.Entities;

public sealed class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
}
