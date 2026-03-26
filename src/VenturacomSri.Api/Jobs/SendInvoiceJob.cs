using Hangfire;
using VenturacomSri.Api.Models;
using VenturacomSri.Api.Services;

namespace VenturacomSri.Api.Jobs;

/// <summary>
/// Job Hangfire: Envía el XML firmado al endpoint de recepción del SRI.
/// Solo funciona si la factura está en estado SIGNED.
/// Tras envío exitoso encola la verificación de autorización.
/// </summary>
public class SendInvoiceJob
{
    private const int CheckAuthDelaySeconds = 15;

    private readonly InvoiceService _invoiceService;
    private readonly ILogger<SendInvoiceJob> _logger;

    public SendInvoiceJob(InvoiceService invoiceService, ILogger<SendInvoiceJob> logger)
    {
        _invoiceService = invoiceService;
        _logger         = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task ExecuteAsync(string invoiceId)
    {
        _logger.LogInformation("Job SEND iniciado para {InvoiceId}", invoiceId);

        var invoice = await _invoiceService.SendToSriAsync(invoiceId);

        if (invoice.Status == InvoiceStatus.Sent)
        {
            // Verificar autorización con delay
            BackgroundJob.Schedule<CheckAuthorizationJob>(
                j => j.ExecuteAsync(invoiceId, 1),
                TimeSpan.FromSeconds(CheckAuthDelaySeconds));

            _logger.LogInformation(
                "Job SEND completado {InvoiceId}. Autorización programada en {Delay}s",
                invoiceId, CheckAuthDelaySeconds);
        }
        else
        {
            _logger.LogWarning(
                "Job SEND: SRI devolvió la factura {InvoiceId} status:{Status}",
                invoiceId, invoice.Status);
        }
    }
}
