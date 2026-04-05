namespace CertificateService.Domain.Entities;

public class CertificadoDigital
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte[] P12Encriptado { get; set; } = Array.Empty<byte>();
    public byte[] PasswordEncriptado { get; set; } = Array.Empty<byte>();
    public string? Thumbprint { get; set; }
    public DateTimeOffset FechaExpiracion { get; set; }
    public bool EstaActivo { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public void Activar() => EstaActivo = true;
    public void Desactivar() => EstaActivo = false;
}
