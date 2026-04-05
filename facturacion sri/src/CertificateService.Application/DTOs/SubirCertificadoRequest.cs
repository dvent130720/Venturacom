namespace CertificateService.Application.DTOs;

public class SubirCertificadoRequest
{
    public Guid TenantId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte[] ArchivoP12 { get; set; } = Array.Empty<byte>();
    public string Password { get; set; } = string.Empty;
}
