using SRI.Facturacion.Domain.Common;
namespace SRI.Facturacion.Domain.Entities;
public sealed class CertificadoDigital : EntityBase { public Guid TenantId { get; set; } public string Thumbprint { get; set; } = string.Empty; public byte[] EncryptedP12 { get; set; } = Array.Empty<byte>(); public byte[] Iv { get; set; } = Array.Empty<byte>(); public bool IsActive { get; set; } = true; }
