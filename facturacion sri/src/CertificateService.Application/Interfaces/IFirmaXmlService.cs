namespace CertificateService.Application.Interfaces;

public interface IFirmaXmlService
{
    Task<string> FirmarXmlAsync(string xml, Guid tenantId, CancellationToken cancellationToken);
}
