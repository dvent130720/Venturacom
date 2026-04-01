using SRI.Facturacion.Domain.Common;
namespace SRI.Facturacion.Domain.Entities;
public sealed class Contribuyente : EntityBase { public Guid TenantId { get; set; } public string RazonSocial { get; set; } = string.Empty; public string NombreComercial { get; set; } = string.Empty; public string DireccionMatriz { get; set; } = string.Empty; public string Ruc { get; set; } = string.Empty; public string Ambiente { get; set; } = "1"; public string TipoEmision { get; set; } = "1"; }
