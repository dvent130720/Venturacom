using SRI.Facturacion.Domain.Common;
namespace SRI.Facturacion.Domain.Entities;
public sealed class Tenant : EntityBase { public string Name { get; set; } = string.Empty; public string Ruc { get; set; } = string.Empty; public bool IsActive { get; set; } = true; public ICollection<Contribuyente> Contribuyentes { get; set; } = new List<Contribuyente>(); }
