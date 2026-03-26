using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenturacomSri.Api.Models;

/// <summary>
/// Certificado electrónico P12.
/// El contenido del P12 y la contraseña se almacenan cifrados (AES-256-GCM).
/// NUNCA se persisten en texto plano.
/// </summary>
[Table("certificates")]
public class Certificate
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(13)]
    public string Ruc { get; set; } = string.Empty;

    /// <summary>Base64 del P12 cifrado con AES-256-GCM.</summary>
    [Required]
    public string EncryptedP12 { get; set; } = string.Empty;

    /// <summary>Contraseña del P12 cifrada con AES-256-GCM.</summary>
    [Required]
    public string EncryptedPassword { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Invoice> Invoices { get; set; } = [];
}
