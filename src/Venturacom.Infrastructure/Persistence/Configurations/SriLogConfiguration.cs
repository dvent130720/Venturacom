using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venturacom.Domain.Entities;

namespace Venturacom.Infrastructure.Persistence.Configurations;

public sealed class SriLogConfiguration : IEntityTypeConfiguration<SriLog>
{
    public void Configure(EntityTypeBuilder<SriLog> builder)
    {
        builder.ToTable("sri_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Direction).HasMaxLength(20).IsRequired();
        builder.Property(x => x.XmlContent).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.InvoiceId, x.LoggedAtUtc });
        builder.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
