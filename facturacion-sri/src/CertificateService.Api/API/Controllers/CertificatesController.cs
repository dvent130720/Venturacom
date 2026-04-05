using CertificateService.Api.API.Contracts;
using CertificateService.Api.Application.Abstractions;
using CertificateService.Api.Application.Certificates.Commands;
using CertificateService.Api.Application.Certificates.Queries;
using CertificateService.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertificateService.Api.API.Controllers;

[ApiController]
[Route("certificates")]
public sealed class CertificatesController(
    UploadCertificateHandler uploadHandler,
    GetActiveCertificateHandler metadataHandler,
    ICertificateRepository repository,
    IEncryptionService encryption,
    IAuditLogger audit) : ControllerBase
{
    [HttpPost("upload")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Upload([FromForm] UploadCertificateRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0) return BadRequest("Archivo .p12 requerido.");
        if (!request.File.FileName.EndsWith(".p12", StringComparison.OrdinalIgnoreCase)) return BadRequest("Solo se permite .p12");
        if (request.File.Length > 4_000_000) return BadRequest("Archivo demasiado grande");

        await using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, ct);

        var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(ms.ToArray(), request.Password,
            System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);

        var id = await uploadHandler.HandleAsync(
            new UploadCertificateCommand(request.TenantId, request.Name, ms.ToArray(), request.Password, x509.NotAfter.ToUniversalTime()), ct);

        audit.Log("certificate.upload", request.TenantId, User?.Identity?.Name ?? "unknown", new { id, request.Name });
        return Created($"/certificates/{request.TenantId}", new { id });
    }

    [HttpGet("{tenantId:guid}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetActive(Guid tenantId, CancellationToken ct)
    {
        var response = await metadataHandler.HandleAsync(new GetActiveCertificateQuery(tenantId), ct);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var cert = await repository.GetByIdAsync(id, ct);
        if (cert is null) return NotFound();
        await repository.DeactivateByTenantAsync(cert.TenantId, ct);
        cert.Activate();
        await repository.SaveChangesAsync(ct);
        audit.Log("certificate.activate", cert.TenantId, User?.Identity?.Name ?? "unknown", new { cert.Id });
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var cert = await repository.GetByIdAsync(id, ct);
        if (cert is null) return NotFound();
        cert.Deactivate();
        await repository.SaveChangesAsync(ct);
        audit.Log("certificate.deactivate", cert.TenantId, User?.Identity?.Name ?? "unknown", new { cert.Id });
        return NoContent();
    }

    [HttpGet("internal/{tenantId:guid}/raw")]
    [Authorize(AuthenticationSchemes = "Internal")]
    public async Task<IActionResult> GetRawForSigner(Guid tenantId, CancellationToken ct)
    {
        var cert = await repository.GetActiveByTenantAsync(tenantId, ct);
        if (cert is null || cert.ExpirationDateUtc <= DateTime.UtcNow) return NotFound();

        audit.Log("certificate.internal_read", tenantId, User?.Identity?.Name ?? "internal", new { cert.Id });
        return Ok(new
        {
            P12Base64 = Convert.ToBase64String(encryption.Decrypt(cert.EncryptedP12)),
            Password = encryption.DecryptString(cert.EncryptedPassword),
            cert.ExpirationDateUtc
        });
    }
}
