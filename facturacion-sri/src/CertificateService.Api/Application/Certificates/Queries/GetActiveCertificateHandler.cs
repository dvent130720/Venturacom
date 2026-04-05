using CertificateService.Api.Application.Abstractions;

namespace CertificateService.Api.Application.Certificates.Queries;

public sealed class GetActiveCertificateHandler(ICertificateRepository repository)
{
    public async Task<CertificateMetadataResponse?> HandleAsync(GetActiveCertificateQuery query, CancellationToken ct)
    {
        var certificate = await repository.GetActiveByTenantAsync(query.TenantId, ct);
        return certificate is null
            ? null
            : new CertificateMetadataResponse(certificate.Id, certificate.TenantId, certificate.Name, certificate.ExpirationDateUtc, certificate.IsActive, certificate.CreatedAtUtc);
    }
}
