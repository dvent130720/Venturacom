using Venturacom.Domain.Common;

namespace Venturacom.Domain.Entities;

public sealed class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
