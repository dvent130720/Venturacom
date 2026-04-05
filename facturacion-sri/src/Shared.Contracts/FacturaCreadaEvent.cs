namespace Shared.Contracts;

public sealed record FacturaCreadaEvent(Guid FacturaId, Guid TenantId, string NumeroComprobante, DateTime CreatedAtUtc);
