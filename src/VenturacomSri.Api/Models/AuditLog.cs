using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenturacomSri.Api.Models;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string InvoiceId { get; set; } = string.Empty;
    public Invoice Invoice { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
