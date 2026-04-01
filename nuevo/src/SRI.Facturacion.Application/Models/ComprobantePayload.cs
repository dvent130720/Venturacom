using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Application;
public sealed record ComprobantePayload(Guid TenantId, TipoComprobante Tipo, string Ruc, string Ambiente, string Serie, string Secuencial, decimal TotalSinImpuestos, decimal ImporteTotal, IReadOnlyCollection<ComprobanteDetalleInput> Detalles, IReadOnlyCollection<ComprobanteImpuestoInput> Impuestos);
public sealed record ComprobanteDetalleInput(string CodigoPrincipal, string Descripcion, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal PrecioTotalSinImpuesto);
public sealed record ComprobanteImpuestoInput(string Codigo, string CodigoPorcentaje, decimal BaseImponible, decimal Tarifa, decimal Valor);
