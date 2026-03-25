using Microsoft.EntityFrameworkCore;
using VenturacomSri.Api.Models;

namespace VenturacomSri.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.AccessKey).IsUnique();
            e.HasIndex(i => i.Status);
            e.HasIndex(i => i.IssuerRuc);
            e.HasIndex(i => i.IssueDate);

            e.HasOne(i => i.Certificate)
             .WithMany(c => c.Invoices)
             .HasForeignKey(i => i.CertificateId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // InvoiceItem
        modelBuilder.Entity<InvoiceItem>(e =>
        {
            e.HasOne(i => i.Invoice)
             .WithMany(inv => inv.Items)
             .HasForeignKey(i => i.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.InvoiceId);

            e.HasOne(a => a.Invoice)
             .WithMany(i => i.AuditLogs)
             .HasForeignKey(a => a.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
