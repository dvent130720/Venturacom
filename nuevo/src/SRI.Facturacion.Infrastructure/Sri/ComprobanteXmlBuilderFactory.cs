using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Infrastructure.Sri;
public sealed class ComprobanteXmlBuilderFactory : IXmlBuilderFactory {
  private readonly IServiceProvider _sp; public ComprobanteXmlBuilderFactory(IServiceProvider sp){_sp=sp;}
  public IComprobanteXmlBuilder Resolve(TipoComprobante tipo)=> tipo switch{
    TipoComprobante.Factura=>_sp.GetRequiredService<FacturaXmlBuilder>(),
    TipoComprobante.NotaCredito=>_sp.GetRequiredService<NotaCreditoXmlBuilder>(),
    TipoComprobante.NotaDebito=>_sp.GetRequiredService<NotaDebitoXmlBuilder>(),
    TipoComprobante.GuiaRemision=>_sp.GetRequiredService<GuiaRemisionXmlBuilder>(),
    TipoComprobante.Retencion=>_sp.GetRequiredService<RetencionXmlBuilder>(),
    _=> throw new NotSupportedException()};
}
