namespace CertificateService.Api.Application.Certificates.Commands;

public sealed record UploadCertificateCommand(Guid TenantId, string Name, byte[] P12Bytes, string Password, DateTime ExpirationDateUtc);
