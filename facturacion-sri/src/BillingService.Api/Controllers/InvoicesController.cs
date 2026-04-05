using BillingService.Api.Domain;
using BillingService.Api.Infrastructure;
using BillingService.Api.Messaging;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;

namespace BillingService.Api.Controllers;

[ApiController]
[Route("invoices")]
public sealed class InvoicesController(BillingDbContext db, IEventPublisher publisher) : ControllerBase
{
    public sealed record CreateInvoiceRequest(Guid TenantId, string NumeroComprobante, decimal Total);

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        var invoice = new Invoice
        {
            TenantId = request.TenantId,
            NumeroComprobante = request.NumeroComprobante,
            Total = request.Total
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        await publisher.PublishAsync(
            new FacturaCreadaEvent(invoice.Id, invoice.TenantId, invoice.NumeroComprobante, invoice.CreatedAtUtc),
            "billing.factura.creada",
            ct);

        return Accepted(new { invoice.Id });
    }
}
