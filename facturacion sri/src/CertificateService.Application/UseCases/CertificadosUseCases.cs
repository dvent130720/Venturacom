using CertificateService.Application.DTOs;
using CertificateService.Application.Interfaces;
using CertificateService.Domain.Entities;
using CertificateService.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace CertificateService.Application.UseCases;

public class CertificadosUseCases : ICertificadosUseCases, ICertificateSigningService
{
    private readonly ICertificadoRepository _repositorio;
    private readonly IEncriptacionService _encriptacion;
    private readonly ICertificadoCriptoService _certificadoCripto;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CertificadosUseCases> _logger;

    public CertificadosUseCases(
        ICertificadoRepository repositorio,
        IEncriptacionService encriptacion,
        ICertificadoCriptoService certificadoCripto,
        IDistributedCache cache,
        ILogger<CertificadosUseCases> logger)
    {
        _repositorio = repositorio;
        _encriptacion = encriptacion;
        _certificadoCripto = certificadoCripto;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CertificadoDto> SubirCertificadoAsync(SubirCertificadoRequest request, CancellationToken cancellationToken)
    {
        if (!request.Nombre.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El archivo debe ser .p12");

        var certificado = _certificadoCripto.CargarYValidar(request.ArchivoP12, request.Password);
        var entidad = new CertificadoDigital
        {
            TenantId = request.TenantId,
            Nombre = request.Nombre,
            P12Encriptado = _encriptacion.Encriptar(request.ArchivoP12),
            PasswordEncriptado = _encriptacion.Encriptar(System.Text.Encoding.UTF8.GetBytes(request.Password)),
            Thumbprint = certificado.Thumbprint,
            FechaExpiracion = certificado.NotAfter,
            EstaActivo = false
        };

        await _repositorio.AgregarAsync(entidad, cancellationToken);
        await _repositorio.GuardarCambiosAsync(cancellationToken);

        _logger.LogInformation("Certificado subido para tenant {TenantId}, certificado {CertificadoId}", request.TenantId, entidad.Id);
        return Mapear(entidad);
    }

    public async Task<IReadOnlyCollection<CertificadoDto>> ListarAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var certificados = await _repositorio.ListarAsync(tenantId, cancellationToken);
        return certificados.Select(Mapear).ToList();
    }

    public async Task<CertificadoDto> ObtenerMetadataAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var certificado = await _repositorio.ObtenerPorIdAsync(tenantId, id, cancellationToken)
            ?? throw new KeyNotFoundException("Certificado no encontrado");

        return Mapear(certificado);
    }

    public async Task ActivarAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var certificado = await _repositorio.ObtenerPorIdAsync(tenantId, id, cancellationToken)
            ?? throw new KeyNotFoundException("Certificado no encontrado");

        await _repositorio.DesactivarTodosAsync(tenantId, cancellationToken);
        certificado.Activar();
        await _repositorio.GuardarCambiosAsync(cancellationToken);

        var cacheKey = $"cert-activo-meta:{tenantId}";
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(new { certificado.Id, certificado.Thumbprint, certificado.FechaExpiracion }),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) }, cancellationToken);

        _logger.LogInformation("Certificado activado tenant={TenantId} certificado={CertificadoId}", tenantId, id);
    }

    public async Task EliminarAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var certificado = await _repositorio.ObtenerPorIdAsync(tenantId, id, cancellationToken)
            ?? throw new KeyNotFoundException("Certificado no encontrado");

        await _repositorio.EliminarAsync(certificado, cancellationToken);
        await _repositorio.GuardarCambiosAsync(cancellationToken);
    }

    public async Task<(X509Certificate2 Certificado, string Password)> ObtenerCertificadoParaFirmaAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var activo = await _repositorio.ObtenerActivoAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("No existe certificado activo para el tenant.");

        var p12 = _encriptacion.Desencriptar(activo.P12Encriptado);
        var password = System.Text.Encoding.UTF8.GetString(_encriptacion.Desencriptar(activo.PasswordEncriptado));
        var cert = _certificadoCripto.CargarYValidar(p12, password);

        return (cert, password);
    }

    private static CertificadoDto Mapear(CertificadoDigital entity)
        => new(entity.Id, entity.TenantId, entity.Nombre, entity.Thumbprint, entity.FechaExpiracion, entity.EstaActivo, entity.CreadoEn);
}
