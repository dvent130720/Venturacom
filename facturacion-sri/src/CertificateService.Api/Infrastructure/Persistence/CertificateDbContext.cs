using CertificateService.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertificateService.Api.Infrastructure.Persistence;

public sealed class CertificateDbContext(DbContextOptions<CertificateDbContext> options) : DbContext(options)
{
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Certificate>(cfg =>
        {
            cfg.ToTable("certificates");
            cfg.HasKey(x => x.Id);
            cfg.Property(x => x.Id).HasColumnName("id");
            cfg.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            cfg.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            cfg.Property(x => x.EncryptedP12).HasColumnName("encrypted_p12").HasColumnType("bytea").IsRequired();
            cfg.Property(x => x.EncryptedPassword).HasColumnName("encrypted_password").HasColumnType("text").IsRequired();
            cfg.Property(x => x.ExpirationDateUtc).HasColumnName("expiration_date").IsRequired();
            cfg.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            cfg.Property(x => x.CreatedAtUtc).HasColumnName("created_at").IsRequired();
            cfg.HasIndex(x => new { x.TenantId, x.IsActive });
        });
    }
}
