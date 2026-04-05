namespace BillingService.Api.Domain;

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
