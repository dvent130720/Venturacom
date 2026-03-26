using Hangfire;
using Microsoft.AspNetCore.Mvc;
using VenturacomSri.Api.DTOs;
using VenturacomSri.Api.Jobs;
using VenturacomSri.Api.Models;
using VenturacomSri.Api.Services;

namespace VenturacomSri.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceService _svc;

    public InvoicesController(InvoiceService svc) => _svc = svc;

    // GET /api/v1/invoices?status=Draft&page=1&limit=20
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page  = 1,
        [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 100);
        var (total, items) = await _svc.ListAsync(status, page, limit);
        return Ok(new { success = true, total, page, limit, invoices = items });
    }

    // POST /api/v1/invoices
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errors = ModelState });

        var invoice = await _svc.CreateAsync(req);

        string? jobId = null;
        string message;

        if (!string.IsNullOrEmpty(invoice.CertificateId))
        {
            jobId = BackgroundJob.Enqueue<SignInvoiceJob>(j => j.ExecuteAsync(invoice.Id));
            message = "Factura creada. Firma y envío encolados automáticamente.";
        }
        else
        {
            message = "Factura creada. Asigne un certificado y llame a POST /invoices/{id}/sign.";
        }

        return CreatedAtAction(nameof(Get), new { id = invoice.Id },
            new { success = true, invoice, jobId, message });
    }

    // GET /api/v1/invoices/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var invoice = await _svc.GetAsync(id);
        return Ok(new { success = true, invoice });
    }

    // POST /api/v1/invoices/{id}/sign
    [HttpPost("{id}/sign")]
    public IActionResult Sign(string id)
    {
        var jobId = BackgroundJob.Enqueue<SignInvoiceJob>(j => j.ExecuteAsync(id));
        return Ok(new { success = true, message = "Job de firma encolado.", jobId });
    }

    // POST /api/v1/invoices/{id}/send
    [HttpPost("{id}/send")]
    public IActionResult Send(string id)
    {
        var jobId = BackgroundJob.Enqueue<SendInvoiceJob>(j => j.ExecuteAsync(id));
        return Ok(new { success = true, message = "Job de envío al SRI encolado.", jobId });
    }

    // POST /api/v1/invoices/{id}/check-auth
    [HttpPost("{id}/check-auth")]
    public IActionResult CheckAuth(string id)
    {
        var jobId = BackgroundJob.Enqueue<CheckAuthorizationJob>(j => j.ExecuteAsync(id, 1));
        return Ok(new { success = true, message = "Consulta de autorización encolada.", jobId });
    }

    // DELETE /api/v1/invoices/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(string id, [FromBody] CancelInvoiceRequest? req)
    {
        var invoice = await _svc.CancelAsync(id, req?.Reason);
        return Ok(new { success = true, invoice });
    }
}
