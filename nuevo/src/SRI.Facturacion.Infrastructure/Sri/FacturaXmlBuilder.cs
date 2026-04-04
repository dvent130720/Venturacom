using System.Xml.Linq;
using SRI.Facturacion.Application;
using SRI.Facturacion.Application.Contracts;
namespace SRI.Facturacion.Infrastructure.Sri;
public sealed class FacturaXmlBuilder : IComprobanteXmlBuilder {
  public string BuildXml(ComprobantePayload payload){
    var doc=new XDocument(new XElement("factura", new XAttribute("id","comprobante"), new XAttribute("version","1.1.0"),
      new XElement("infoTributaria", new XElement("ambiente",payload.Ambiente), new XElement("razonSocial","EMISOR"), new XElement("ruc",payload.Ruc), new XElement("codDoc",((int)payload.Tipo).ToString("00")), new XElement("estab",payload.Serie[..3]), new XElement("ptoEmi",payload.Serie[3..]), new XElement("secuencial",payload.Secuencial.PadLeft(9,'0'))),
      new XElement("infoFactura", new XElement("fechaEmision",DateTime.UtcNow.ToString("dd/MM/yyyy")), new XElement("totalSinImpuestos",payload.TotalSinImpuestos), new XElement("importeTotal",payload.ImporteTotal)),
      new XElement("detalles", payload.Detalles.Select(d=>new XElement("detalle", new XElement("codigoPrincipal",d.CodigoPrincipal), new XElement("descripcion",d.Descripcion), new XElement("cantidad",d.Cantidad), new XElement("precioUnitario",d.PrecioUnitario), new XElement("descuento",d.Descuento), new XElement("precioTotalSinImpuesto",d.PrecioTotalSinImpuesto)) ))
    ));
    return doc.ToString(SaveOptions.DisableFormatting);
  }
  public bool Validate(string xml, out IReadOnlyCollection<string> errors){ var list=new List<string>(); if(!xml.Contains("<factura")) list.Add("XML inválido para factura"); errors=list; return list.Count==0; }
}
