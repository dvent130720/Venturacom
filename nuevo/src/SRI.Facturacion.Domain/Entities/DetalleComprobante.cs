using SRI.Facturacion.Domain.Common;
namespace SRI.Facturacion.Domain.Entities;
public sealed class DetalleComprobante : EntityBase { public Guid ComprobanteId { get; set; } public string CodigoPrincipal { get; set; } = string.Empty; public string Descripcion { get; set; } = string.Empty; public decimal Cantidad { get; set; } public decimal PrecioUnitario { get; set; } public decimal Descuento { get; set; } public decimal PrecioTotalSinImpuesto { get; set; } }
