using CertificateService.Api.Application.Abstractions;
using CertificateService.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertificateService.Api.Infrastructure.Persistence;

public sealed class CertificateRepository(CertificateDbContext db) : ICertificateRepository
{
    public Task AddAsync(Certificate certificate, CancellationToken ct) => db.Certificates.AddAsync(certificate, ct).AsTask();

    public Task<Certificate?> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct) =>
        db.Certificates.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsActive, ct);

    public Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct) => db.Certificates.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task DeactivateByTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var certificates = await db.Certificates.Where(x => x.TenantId == tenantId && x.IsActive).ToListAsync(ct);
        foreach (var certificate in certificates) certificate.Deactivate();
    }
}
