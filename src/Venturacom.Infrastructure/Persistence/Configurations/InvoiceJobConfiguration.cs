using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venturacom.Domain.Entities;

namespace Venturacom.Infrastructure.Persistence.Configurations;

public sealed class InvoiceJobConfiguration : IEntityTypeConfiguration<InvoiceJob>
{
    public void Configure(EntityTypeBuilder<InvoiceJob> builder)
    {
        builder.ToTable("invoice_jobs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.Status, x.ScheduledAtUtc });
        builder.HasOne(x => x.Invoice).WithMany(x => x.Jobs).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
