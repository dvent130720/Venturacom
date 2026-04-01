using Microsoft.EntityFrameworkCore;
using SRI.Facturacion.Domain.Entities;
namespace SRI.Facturacion.Infrastructure.Persistence;
public sealed class AppDbContext : DbContext {
  public AppDbContext(DbContextOptions<AppDbContext> options):base(options){}
  public DbSet<Tenant> Tenants => Set<Tenant>(); public DbSet<Contribuyente> Contribuyentes => Set<Contribuyente>(); public DbSet<CertificadoDigital> Certificados => Set<CertificadoDigital>(); public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
  protected override void OnModelCreating(ModelBuilder b){ b.Entity<Comprobante>().HasIndex(x=>new{x.TenantId,x.ClaveAcceso}).IsUnique(); b.Entity<Comprobante>().HasMany(x=>x.Detalles).WithOne().HasForeignKey(x=>x.ComprobanteId); b.Entity<Comprobante>().HasMany(x=>x.Impuestos).WithOne().HasForeignKey(x=>x.ComprobanteId); }
}
