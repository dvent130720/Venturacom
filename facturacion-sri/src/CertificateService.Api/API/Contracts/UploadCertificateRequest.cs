namespace CertificateService.Api.API.Contracts;

public sealed class UploadCertificateRequest
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}
