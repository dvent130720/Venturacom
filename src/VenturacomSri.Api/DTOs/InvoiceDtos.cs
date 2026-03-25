using System.ComponentModel.DataAnnotations;

namespace VenturacomSri.Api.DTOs;

// ── Request ───────────────────────────────────────────────────────
public class CreateInvoiceRequest
{
    public DateTime? IssueDate { get; set; }

    /// <summary>04=RUC, 05=cédula, 06=pasaporte, 07=consumidor final</summary>
    [Required, RegularExpression("^(04|05|06|07)$")]
    public string BuyerIdType { get; set; } = "05";

    [Required, MaxLength(20)]
    public string BuyerId { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string BuyerName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string? BuyerEmail { get; set; }

    [MaxLength(300)]
    public string? BuyerAddress { get; set; }

    /// <summary>01=efectivo, 19=tarjeta de crédito, etc.</summary>
    [RegularExpression("^(01|15|16|17|18|19|20|21)$")]
    public string PaymentMethod { get; set; } = "01";

    public string? CertificateId { get; set; }

    [Required, MinLength(1)]
    public List<CreateInvoiceItemRequest> Items { get; set; } = [];
}

public class CreateInvoiceItemRequest
{
    [MaxLength(25)]
    public string? MainCode { get; set; }

    [MaxLength(25)]
    public string? AuxCode { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [Range(0.000001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0.000001, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }

    /// <summary>0=0%, 2=12%, 3=14%, 6=no objeto, 7=exento, 10=15%</summary>
    [RegularExpression("^(0|2|3|6|7|10)$")]
    public string TaxPercentageCode { get; set; } = "2";
}

public class CancelInvoiceRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
