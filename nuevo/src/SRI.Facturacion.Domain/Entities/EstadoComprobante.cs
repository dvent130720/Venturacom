using SRI.Facturacion.Domain.Common;
using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Domain.Entities;
public sealed class EstadoComprobante : EntityBase { public Guid ComprobanteId { get; set; } public EstadoComprobanteEnum Estado { get; set; } public string? CodigoSri { get; set; } public string? MensajeSri { get; set; } }
