using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenturacomSri.Api.Models;

[Table("invoices")]
public class Invoice
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Clave de acceso SRI — 49 dígitos.</summary>
    [Required, MaxLength(49), MinLength(49)]
    public string AccessKey { get; set; } = string.Empty;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>1 = Pruebas, 2 = Producción.</summary>
    public int Environment { get; set; } = 1;

    // ── Emisor ──────────────────────────────────────────────────────
    [Required, MaxLength(13)]
    public string IssuerRuc { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string IssuerName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? IssuerTradeName { get; set; }

    [Required, MaxLength(300)]
    public string IssuerAddress { get; set; } = string.Empty;

    [Required, MaxLength(3)]
    public string Establishment { get; set; } = string.Empty;

    [Required, MaxLength(3)]
    public string EmissionPoint { get; set; } = string.Empty;

    [Required, MaxLength(9)]
    public string Sequential { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    // ── Receptor ────────────────────────────────────────────────────
    /// <summary>04=RUC, 05=cédula, 06=pasaporte, 07=consumidor final</summary>
    [Required, MaxLength(2)]
    public string BuyerIdType { get; set; } = "05";

    [Required, MaxLength(20)]
    public string BuyerId { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string BuyerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? BuyerEmail { get; set; }

    [MaxLength(300)]
    public string? BuyerAddress { get; set; }

    // ── Totales ─────────────────────────────────────────────────────
    [Column(TypeName = "numeric(14,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal TotalDiscount { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal TotalIva { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal Tip { get; set; }

    [MaxLength(20)]
    public string Currency { get; set; } = "DOLAR";

    [MaxLength(2)]
    public string PaymentMethod { get; set; } = "01";

    [Column(TypeName = "numeric(14,2)")]
    public decimal PaymentAmount { get; set; }

    // ── XML ─────────────────────────────────────────────────────────
    public string? XmlContent { get; set; }
    public string? SignedXml { get; set; }
    public string? SriResponse { get; set; }
    public string? AuthXml { get; set; }

    // ── Autorización ────────────────────────────────────────────────
    [MaxLength(49)]
    public string? AuthNumber { get; set; }
    public DateTime? AuthDate { get; set; }
    public string? RejectReason { get; set; }

    // ── Relaciones ──────────────────────────────────────────────────
    public string? CertificateId { get; set; }
    public Certificate? Certificate { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
