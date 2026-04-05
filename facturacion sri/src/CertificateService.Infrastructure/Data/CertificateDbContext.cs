using CertificateService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertificateService.Infrastructure.Data;

public class CertificateDbContext(DbContextOptions<CertificateDbContext> options) : DbContext(options)
{
    public DbSet<CertificadoDigital> Certificados => Set<CertificadoDigital>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var certificado = modelBuilder.Entity<CertificadoDigital>();
        certificado.ToTable("certificates");
        certificado.HasKey(x => x.Id);
        certificado.Property(x => x.Id).HasColumnName("id");
        certificado.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        certificado.Property(x => x.Nombre).HasColumnName("name").HasMaxLength(200).IsRequired();
        certificado.Property(x => x.P12Encriptado).HasColumnName("encrypted_p12").HasColumnType("bytea").IsRequired();
        certificado.Property(x => x.PasswordEncriptado).HasColumnName("encrypted_password").HasColumnType("bytea").IsRequired();
        certificado.Property(x => x.Thumbprint).HasColumnName("thumbprint").HasMaxLength(200);
        certificado.Property(x => x.FechaExpiracion).HasColumnName("expiration_date").IsRequired();
        certificado.Property(x => x.EstaActivo).HasColumnName("is_active").IsRequired();
        certificado.Property(x => x.CreadoEn).HasColumnName("created_at").IsRequired();

        certificado.HasIndex(x => x.TenantId);
        certificado.HasIndex(x => x.EstaActivo);
    }
}
