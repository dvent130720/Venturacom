using SRI.Facturacion.Domain.Common;
namespace SRI.Facturacion.Domain.Entities;
public sealed class Impuesto : EntityBase { public Guid ComprobanteId { get; set; } public string Codigo { get; set; } = string.Empty; public string CodigoPorcentaje { get; set; } = string.Empty; public decimal BaseImponible { get; set; } public decimal Tarifa { get; set; } public decimal Valor { get; set; } }
