using BillingService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Api.Infrastructure;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(cfg =>
        {
            cfg.ToTable("invoices");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.TenantId).HasColumnName("tenant_id");
            cfg.Property(x => x.NumeroComprobante).HasColumnName("numero_comprobante").HasMaxLength(49);
            cfg.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
            cfg.Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        });
    }
}
