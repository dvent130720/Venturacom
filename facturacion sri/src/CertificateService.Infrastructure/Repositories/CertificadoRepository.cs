using CertificateService.Domain.Entities;
using CertificateService.Domain.Interfaces;
using CertificateService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CertificateService.Infrastructure.Repositories;

public class CertificadoRepository(CertificateDbContext dbContext) : ICertificadoRepository
{
    public async Task AgregarAsync(CertificadoDigital certificado, CancellationToken cancellationToken)
        => await dbContext.Certificados.AddAsync(certificado, cancellationToken);

    public async Task<CertificadoDigital?> ObtenerPorIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => await dbContext.Certificados.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<CertificadoDigital>> ListarAsync(Guid tenantId, CancellationToken cancellationToken)
        => await dbContext.Certificados.Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreadoEn).ToListAsync(cancellationToken);

    public async Task<CertificadoDigital?> ObtenerActivoAsync(Guid tenantId, CancellationToken cancellationToken)
        => await dbContext.Certificados.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.EstaActivo, cancellationToken);

    public async Task DesactivarTodosAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var activos = await dbContext.Certificados.Where(x => x.TenantId == tenantId && x.EstaActivo).ToListAsync(cancellationToken);
        foreach (var cert in activos)
            cert.Desactivar();
    }

    public Task EliminarAsync(CertificadoDigital certificado, CancellationToken cancellationToken)
    {
        dbContext.Certificados.Remove(certificado);
        return Task.CompletedTask;
    }

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken)
        => await dbContext.SaveChangesAsync(cancellationToken);
}
