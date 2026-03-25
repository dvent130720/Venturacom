using Hangfire;
using VenturacomSri.Api.Models;
using VenturacomSri.Api.Services;

namespace VenturacomSri.Api.Jobs;

/// <summary>
/// Job Hangfire: Consulta el estado de autorización en el SRI.
/// Si aún no está autorizada (SENT), reintenta hasta MaxAttempts veces.
/// </summary>
public class CheckAuthorizationJob
{
    private const int MaxAttempts      = 10;
    private const int RetryDelaySeconds = 30;

    private readonly InvoiceService _invoiceService;
    private readonly ILogger<CheckAuthorizationJob> _logger;

    public CheckAuthorizationJob(InvoiceService invoiceService, ILogger<CheckAuthorizationJob> logger)
    {
        _invoiceService = invoiceService;
        _logger         = logger;
    }

    [AutomaticRetry(Attempts = 0)] // Reintento manual con delay
    public async Task ExecuteAsync(string invoiceId, int attempt)
    {
        _logger.LogInformation(
            "Job CHECK-AUTH {InvoiceId} intento:{Attempt}/{Max}",
            invoiceId, attempt, MaxAttempts);

        var invoice = await _invoiceService.CheckAuthorizationAsync(invoiceId);

        if (invoice.Status == InvoiceStatus.Authorized)
        {
            _logger.LogInformation(
                "Factura AUTORIZADA {InvoiceId} auth:{Num}",
                invoiceId, invoice.AuthNumber);
        }
        else if (invoice.Status == InvoiceStatus.Sent && attempt < MaxAttempts)
        {
            // SRI aún procesa; reintentar
            BackgroundJob.Schedule<CheckAuthorizationJob>(
                j => j.ExecuteAsync(invoiceId, attempt + 1),
                TimeSpan.FromSeconds(RetryDelaySeconds));

            _logger.LogDebug(
                "Autorización pendiente {InvoiceId}. Próximo intento en {Delay}s",
                invoiceId, RetryDelaySeconds);
        }
        else if (attempt >= MaxAttempts)
        {
            _logger.LogError(
                "Máximo de intentos ({Max}) de autorización alcanzado para {InvoiceId}",
                MaxAttempts, invoiceId);
        }
    }
}
