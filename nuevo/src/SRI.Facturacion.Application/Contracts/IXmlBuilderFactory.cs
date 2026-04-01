using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Application.Contracts;
public interface IXmlBuilderFactory { IComprobanteXmlBuilder Resolve(TipoComprobante tipo); }
public interface IComprobanteXmlBuilder { string BuildXml(ComprobantePayload payload); bool Validate(string xml, out IReadOnlyCollection<string> errors); }
