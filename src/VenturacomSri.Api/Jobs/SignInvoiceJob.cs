using Hangfire;
using VenturacomSri.Api.Services;

namespace VenturacomSri.Api.Jobs;

/// <summary>
/// Job Hangfire: Firma el XML del comprobante con XAdES-BES.
/// Tras firmar exitosamente, encola automáticamente el envío al SRI.
/// </summary>
public class SignInvoiceJob
{
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<SignInvoiceJob> _logger;

    public SignInvoiceJob(InvoiceService invoiceService, ILogger<SignInvoiceJob> logger)
    {
        _invoiceService = invoiceService;
        _logger         = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync(string invoiceId)
    {
        _logger.LogInformation("Job SIGN iniciado para {InvoiceId}", invoiceId);

        var invoice = await _invoiceService.SignAsync(invoiceId);

        // Encolar el envío automáticamente
        BackgroundJob.Enqueue<SendInvoiceJob>(j => j.ExecuteAsync(invoiceId));

        _logger.LogInformation(
            "Job SIGN completado {InvoiceId} status:{Status}",
            invoiceId, invoice.Status);
    }
}
