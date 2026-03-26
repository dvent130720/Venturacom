using System.ComponentModel.DataAnnotations;

namespace VenturacomSri.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(256)]
    public required string Email { get; set; }

    [MaxLength(256)]
    public string? Name { get; set; }

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }

    // Proveedor: "email" | "google"
    [MaxLength(32)]
    public required string Provider { get; set; }

    // ID externo de Google (null si proveedor = email)
    [MaxLength(256)]
    public string? GoogleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}
