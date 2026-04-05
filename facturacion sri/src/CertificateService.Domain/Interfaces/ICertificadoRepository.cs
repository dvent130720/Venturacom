using CertificateService.Domain.Entities;

namespace CertificateService.Domain.Interfaces;

public interface ICertificadoRepository
{
    Task AgregarAsync(CertificadoDigital certificado, CancellationToken cancellationToken);
    Task<CertificadoDigital?> ObtenerPorIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CertificadoDigital>> ListarAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<CertificadoDigital?> ObtenerActivoAsync(Guid tenantId, CancellationToken cancellationToken);
    Task DesactivarTodosAsync(Guid tenantId, CancellationToken cancellationToken);
    Task EliminarAsync(CertificadoDigital certificado, CancellationToken cancellationToken);
    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
