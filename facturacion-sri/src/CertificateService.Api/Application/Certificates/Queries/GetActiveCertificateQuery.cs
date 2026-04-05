namespace CertificateService.Api.Application.Certificates.Queries;

public sealed record GetActiveCertificateQuery(Guid TenantId);

public sealed record CertificateMetadataResponse(Guid Id, Guid TenantId, string Name, DateTime ExpirationDateUtc, bool IsActive, DateTime CreatedAtUtc);
