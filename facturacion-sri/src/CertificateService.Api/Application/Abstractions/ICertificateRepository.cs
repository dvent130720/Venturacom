using CertificateService.Api.Domain.Entities;

namespace CertificateService.Api.Application.Abstractions;

public interface ICertificateRepository
{
    Task AddAsync(Certificate certificate, CancellationToken ct);
    Task<Certificate?> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct);
    Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct);
    Task DeactivateByTenantAsync(Guid tenantId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
