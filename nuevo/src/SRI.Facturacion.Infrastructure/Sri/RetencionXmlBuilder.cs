using SRI.Facturacion.Application.Contracts;
namespace SRI.Facturacion.Infrastructure.Sri;
public sealed class RetencionXmlBuilder : IComprobanteXmlBuilder {
  private readonly FacturaXmlBuilder _inner = new();
  public string BuildXml(SRI.Facturacion.Application.ComprobantePayload payload) => _inner.BuildXml(payload);
  public bool Validate(string xml, out IReadOnlyCollection<string> errors) => _inner.Validate(xml, out errors);
}
