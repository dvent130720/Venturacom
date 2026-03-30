using Venturacom.Domain.Entities;

namespace Venturacom.Infrastructure.Services;

public interface ISriXmlService
{
    string GenerateInvoiceXml(Invoice invoice);
    Task<(bool Authorized, string ResponseXml)> SendAsync(string xml, CancellationToken cancellationToken);
}

public sealed class SriXmlService : ISriXmlService
{
    public string GenerateInvoiceXml(Invoice invoice) =>
        $"<factura id=\"{invoice.Id}\" numero=\"{invoice.Number}\" total=\"{invoice.Total}\" />";

    public Task<(bool Authorized, string ResponseXml)> SendAsync(string xml, CancellationToken cancellationToken)
    {
        var authorized = Random.Shared.Next(1, 100) > 15;
        var response = authorized
            ? $"<respuesta estado=\"AUTORIZADO\">{xml}</respuesta>"
            : $"<respuesta estado=\"RECHAZADO\">{xml}</respuesta>";
        return Task.FromResult((authorized, response));
    }
}
