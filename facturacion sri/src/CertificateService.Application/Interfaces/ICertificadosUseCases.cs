using CertificateService.Application.DTOs;

namespace CertificateService.Application.Interfaces;

public interface ICertificadosUseCases
{
    Task<CertificadoDto> SubirCertificadoAsync(SubirCertificadoRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CertificadoDto>> ListarAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<CertificadoDto> ObtenerMetadataAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task ActivarAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task EliminarAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
}
