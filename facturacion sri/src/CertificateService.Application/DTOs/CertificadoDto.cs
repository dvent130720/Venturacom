namespace CertificateService.Application.DTOs;

public record CertificadoDto(
    Guid Id,
    Guid TenantId,
    string Nombre,
    string? Thumbprint,
    DateTimeOffset FechaExpiracion,
    bool EstaActivo,
    DateTimeOffset CreadoEn
);
