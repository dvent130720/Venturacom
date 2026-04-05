using System.Security.Cryptography.X509Certificates;

namespace CertificateService.Application.Interfaces;

public interface ICertificateSigningService
{
    Task<(X509Certificate2 Certificado, string Password)> ObtenerCertificadoParaFirmaAsync(Guid tenantId, CancellationToken cancellationToken);
}
