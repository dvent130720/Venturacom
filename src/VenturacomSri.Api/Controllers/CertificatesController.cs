using Microsoft.AspNetCore.Mvc;
using VenturacomSri.Api.Services;

namespace VenturacomSri.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly CertificateService _svc;

    public CertificatesController(CertificateService svc) => _svc = svc;

    // GET /api/v1/certificates
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var certs = await _svc.ListAsync();
        return Ok(new { success = true, certificates = certs });
    }

    // POST /api/v1/certificates
    // multipart/form-data: certificate (.p12), password, name
    [HttpPost]
    [RequestSizeLimit(1_048_576)] // 1 MB
    public async Task<IActionResult> Upload(
        IFormFile certificate,
        [FromForm] string password,
        [FromForm] string? name)
    {
        if (certificate is null || certificate.Length == 0)
            return BadRequest(new { success = false, error = "Se requiere el archivo P12." });

        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new { success = false, error = "Se requiere la contraseña del P12." });

        var ext = Path.GetExtension(certificate.FileName).ToLowerInvariant();
        if (ext is not (".p12" or ".pfx"))
            return BadRequest(new { success = false, error = "Solo se aceptan archivos .p12 o .pfx." });

        using var ms = new MemoryStream();
        await certificate.CopyToAsync(ms);
        var p12Buffer = ms.ToArray();

        var cert = await _svc.UploadAsync(
            name ?? certificate.FileName,
            p12Buffer,
            password);

        return CreatedAtAction(nameof(List), null, new
        {
            success = true,
            certificate = new
            {
                cert.Id, cert.Name, cert.Ruc,
                cert.ValidFrom, cert.ValidUntil, cert.Active
            }
        });
    }

    // PATCH /api/v1/certificates/{id}/activate
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(string id)
    {
        var cert = await _svc.SetActiveAsync(id, true);
        return Ok(new { success = true, certificate = new { cert.Id, cert.Active } });
    }

    // PATCH /api/v1/certificates/{id}/deactivate
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var cert = await _svc.SetActiveAsync(id, false);
        return Ok(new { success = true, certificate = new { cert.Id, cert.Active } });
    }
}
