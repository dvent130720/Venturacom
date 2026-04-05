using CertificateService.API.Extensions;
using CertificateService.Application.DTOs;
using CertificateService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CertificateService.API.Controllers;

[ApiController]
[Route("certificates")]
public class CertificadosController(ICertificadosUseCases useCases, IFirmaXmlService firmaXmlService, ILogger<CertificadosController> logger) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Subir([FromForm] IFormFile file, [FromForm] string password, [FromForm] string name, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.ObtenerTenantId();

        if (file.Length == 0)
            return BadRequest("Archivo vacío.");

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);

        var request = new SubirCertificadoRequest
        {
            TenantId = tenantId,
            Nombre = name,
            Password = password,
            ArchivoP12 = ms.ToArray()
        };

        var response = await useCases.SubirCertificadoAsync(request, cancellationToken);
        logger.LogInformation("Carga de certificado completada para tenant {TenantId}", tenantId);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.ObtenerTenantId();
        return Ok(await useCases.ListarAsync(tenantId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.ObtenerTenantId();
        return Ok(await useCases.ObtenerMetadataAsync(tenantId, id, cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.ObtenerTenantId();
        await useCases.ActivarAsync(tenantId, id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.ObtenerTenantId();
        await useCases.EliminarAsync(tenantId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("sign-xml")]
    public async Task<IActionResult> FirmarXml([FromBody] string xml, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.ObtenerTenantId();
        var firmado = await firmaXmlService.FirmarXmlAsync(xml, tenantId, cancellationToken);
        return Ok(new { xmlFirmado = firmado });
    }
}
