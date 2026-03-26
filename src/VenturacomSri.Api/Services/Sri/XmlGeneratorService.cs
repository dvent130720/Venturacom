using System.Xml;
using VenturacomSri.Api.Models;

namespace VenturacomSri.Api.Services.Sri;

/// <summary>
/// Genera el XML de facturas electrónicas según el esquema SRI Ecuador v2.1.0.
/// </summary>
public class XmlGeneratorService
{
    public string GenerateInvoiceXml(Invoice invoice)
    {
        var doc = new XmlDocument();
        var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.AppendChild(declaration);

        // <factura id="comprobante" version="2.1.0">
        var root = doc.CreateElement("factura");
        root.SetAttribute("id", "comprobante");
        root.SetAttribute("version", "2.1.0");
        doc.AppendChild(root);

        // ── infoTributaria ───────────────────────────────────────────
        var infoTrib = root.AppendChild(doc.CreateElement("infoTributaria"));
        infoTrib.AppendText(doc, "ambiente", invoice.Environment.ToString());
        infoTrib.AppendText(doc, "tipoEmision", "1");
        infoTrib.AppendText(doc, "razonSocial", invoice.IssuerName);
        if (!string.IsNullOrWhiteSpace(invoice.IssuerTradeName))
            infoTrib.AppendText(doc, "nombreComercial", invoice.IssuerTradeName);
        infoTrib.AppendText(doc, "ruc", invoice.IssuerRuc);
        infoTrib.AppendText(doc, "claveAcceso", invoice.AccessKey);
        infoTrib.AppendText(doc, "codDoc", "01");
        infoTrib.AppendText(doc, "estab", invoice.Establishment);
        infoTrib.AppendText(doc, "ptoEmi", invoice.EmissionPoint);
        infoTrib.AppendText(doc, "secuencial", invoice.Sequential);
        infoTrib.AppendText(doc, "dirMatriz", invoice.IssuerAddress);

        // ── infoFactura ──────────────────────────────────────────────
        var infoFact = root.AppendChild(doc.CreateElement("infoFactura"));
        infoFact.AppendText(doc, "fechaEmision", invoice.IssueDate.ToString("dd/MM/yyyy"));
        infoFact.AppendText(doc, "dirEstablecimiento", invoice.IssuerAddress);
        infoFact.AppendText(doc, "obligadoContabilidad", "SI");
        infoFact.AppendText(doc, "tipoIdentificacionComprador", invoice.BuyerIdType);
        infoFact.AppendText(doc, "razonSocialComprador", invoice.BuyerName);
        infoFact.AppendText(doc, "identificacionComprador", invoice.BuyerId);
        infoFact.AppendText(doc, "totalSinImpuestos", invoice.Subtotal.ToString("F2"));
        infoFact.AppendText(doc, "totalDescuento", invoice.TotalDiscount.ToString("F2"));

        // totalConImpuestos (agrupado)
        var totalConImp = infoFact.AppendChild(doc.CreateElement("totalConImpuestos"));
        foreach (var grp in GroupTaxes(invoice.Items))
        {
            var ti = totalConImp.AppendChild(doc.CreateElement("totalImpuesto"));
            ti.AppendText(doc, "codigo", grp.Code);
            ti.AppendText(doc, "codigoPorcentaje", grp.PercentageCode);
            ti.AppendText(doc, "descuentoAdicional", "0.00");
            ti.AppendText(doc, "baseImponible", grp.Base.ToString("F2"));
            ti.AppendText(doc, "valor", grp.Value.ToString("F2"));
        }

        infoFact.AppendText(doc, "propina", invoice.Tip.ToString("F2"));
        infoFact.AppendText(doc, "importeTotal", invoice.TotalAmount.ToString("F2"));
        infoFact.AppendText(doc, "moneda", invoice.Currency);

        var pagos = infoFact.AppendChild(doc.CreateElement("pagos"));
        var pago = pagos.AppendChild(doc.CreateElement("pago"));
        pago.AppendText(doc, "formaPago", invoice.PaymentMethod);
        pago.AppendText(doc, "total", invoice.PaymentAmount.ToString("F2"));
        pago.AppendText(doc, "plazo", "0");
        pago.AppendText(doc, "unidadTiempo", "dias");

        // ── detalles ─────────────────────────────────────────────────
        var detalles = root.AppendChild(doc.CreateElement("detalles"));
        foreach (var item in invoice.Items)
        {
            var det = detalles.AppendChild(doc.CreateElement("detalle"));
            if (!string.IsNullOrWhiteSpace(item.MainCode))
                det.AppendText(doc, "codigoPrincipal", item.MainCode);
            if (!string.IsNullOrWhiteSpace(item.AuxCode))
                det.AppendText(doc, "codigoAuxiliar", item.AuxCode);
            det.AppendText(doc, "descripcion", item.Description);
            det.AppendText(doc, "cantidad", item.Quantity.ToString("F6"));
            det.AppendText(doc, "precioUnitario", item.UnitPrice.ToString("F6"));
            det.AppendText(doc, "descuento", item.Discount.ToString("F2"));
            det.AppendText(doc, "precioTotalSinImpuesto", item.SubtotalNoTax.ToString("F2"));

            var impuestos = det.AppendChild(doc.CreateElement("impuestos"));
            var imp = impuestos.AppendChild(doc.CreateElement("impuesto"));
            imp.AppendText(doc, "codigo", item.TaxCode);
            imp.AppendText(doc, "codigoPorcentaje", item.TaxPercentageCode);
            imp.AppendText(doc, "tarifa", item.TaxRate.ToString("F2"));
            imp.AppendText(doc, "baseImponible", item.TaxBase.ToString("F2"));
            imp.AppendText(doc, "valor", item.TaxValue.ToString("F2"));
        }

        // ── infoAdicional ────────────────────────────────────────────
        var infoAdi = root.AppendChild(doc.CreateElement("infoAdicional"));
        if (!string.IsNullOrWhiteSpace(invoice.BuyerEmail))
        {
            var campo = doc.CreateElement("campoAdicional");
            campo.SetAttribute("nombre", "email");
            campo.InnerText = invoice.BuyerEmail;
            infoAdi.AppendChild(campo);
        }
        if (!string.IsNullOrWhiteSpace(invoice.BuyerAddress))
        {
            var campo = doc.CreateElement("campoAdicional");
            campo.SetAttribute("nombre", "direccion");
            campo.InnerText = invoice.BuyerAddress;
            infoAdi.AppendChild(campo);
        }

        return doc.OuterXml;
    }

    // ── Helpers ──────────────────────────────────────────────────────
    private record TaxGroup(string Code, string PercentageCode, decimal Base, decimal Value);

    private static List<TaxGroup> GroupTaxes(ICollection<InvoiceItem> items)
    {
        return items
            .GroupBy(i => (i.TaxCode, i.TaxPercentageCode))
            .Select(g => new TaxGroup(
                g.Key.TaxCode,
                g.Key.TaxPercentageCode,
                g.Sum(i => i.TaxBase),
                g.Sum(i => i.TaxValue)))
            .ToList();
    }
}

file static class XmlNodeExtensions
{
    internal static XmlNode AppendText(this XmlNode parent, XmlDocument doc, string tag, string text)
    {
        var el = doc.CreateElement(tag);
        el.InnerText = text;
        return parent.AppendChild(el);
    }
}
