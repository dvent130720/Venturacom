using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenturacomSri.Api.Models;

[Table("invoice_items")]
public class InvoiceItem
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string InvoiceId { get; set; } = string.Empty;
    public Invoice Invoice { get; set; } = null!;

    [MaxLength(25)]
    public string? MainCode { get; set; }

    [MaxLength(25)]
    public string? AuxCode { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "numeric(14,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "numeric(14,6)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal Discount { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal SubtotalNoTax { get; set; }

    // IVA
    [MaxLength(2)]
    public string TaxCode { get; set; } = "2";

    /// <summary>0=0%, 2=12%, 3=14%, 6=no objeto, 7=exento, 10=15%</summary>
    [MaxLength(2)]
    public string TaxPercentageCode { get; set; } = "2";

    [Column(TypeName = "numeric(5,2)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal TaxBase { get; set; }

    [Column(TypeName = "numeric(14,2)")]
    public decimal TaxValue { get; set; }
}
