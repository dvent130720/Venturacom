/**
 * Generador de XML para Facturas Electrónicas SRI Ecuador
 * Versión del esquema: 2.1.0
 * Ref: https://www.sri.gob.ec/facturacion-electronica
 */
import { create } from "xmlbuilder2";
import { Invoice, InvoiceItem } from "@prisma/client";
import Decimal from "decimal.js";

type InvoiceWithItems = Invoice & { items: InvoiceItem[] };

function fmt(value: Decimal | string | number, decimals = 2): string {
  return new Decimal(value).toFixed(decimals);
}

function formatDate(date: Date): string {
  const d = date.getDate().toString().padStart(2, "0");
  const m = (date.getMonth() + 1).toString().padStart(2, "0");
  const y = date.getFullYear();
  return `${d}/${m}/${y}`;
}

export function generateInvoiceXml(invoice: InvoiceWithItems): string {
  const root = create({ version: "1.0", encoding: "UTF-8" })
    .ele("factura", { id: "comprobante", version: "2.1.0" });

  // ── infoTributaria ──────────────────────────────────────────────
  const infoTributaria = root.ele("infoTributaria");
  infoTributaria.ele("ambiente").txt(String(invoice.environment));
  infoTributaria.ele("tipoEmision").txt("1");
  infoTributaria.ele("razonSocial").txt(invoice.issuerName);
  if (invoice.issuerTradeName) {
    infoTributaria.ele("nombreComercial").txt(invoice.issuerTradeName);
  }
  infoTributaria.ele("ruc").txt(invoice.issuerRuc);
  infoTributaria.ele("claveAcceso").txt(invoice.accessKey);
  infoTributaria.ele("codDoc").txt("01");
  infoTributaria.ele("estab").txt(invoice.establishment);
  infoTributaria.ele("ptoEmi").txt(invoice.emissionPoint);
  infoTributaria.ele("secuencial").txt(invoice.sequential);
  infoTributaria.ele("dirMatriz").txt(invoice.issuerAddress);

  // ── infoFactura ─────────────────────────────────────────────────
  const infoFactura = root.ele("infoFactura");
  infoFactura.ele("fechaEmision").txt(formatDate(invoice.issueDate));
  infoFactura.ele("dirEstablecimiento").txt(invoice.issuerAddress);
  infoFactura.ele("obligadoContabilidad").txt("SI");
  infoFactura.ele("tipoIdentificacionComprador").txt(invoice.buyerIdType);
  infoFactura.ele("razonSocialComprador").txt(invoice.buyerName);
  infoFactura.ele("identificacionComprador").txt(invoice.buyerId);
  infoFactura.ele("totalSinImpuestos").txt(fmt(invoice.subtotal));
  infoFactura.ele("totalDescuento").txt(fmt(invoice.totalDiscount));

  // totalConImpuestos (agrupado por código de impuesto)
  const taxGroups = groupTaxes(invoice.items);
  const totalConImpuestos = infoFactura.ele("totalConImpuestos");
  for (const tax of taxGroups) {
    const totalImpuesto = totalConImpuestos.ele("totalImpuesto");
    totalImpuesto.ele("codigo").txt(tax.code);
    totalImpuesto.ele("codigoPorcentaje").txt(tax.percentageCode);
    totalImpuesto.ele("descuentoAdicional").txt("0.00");
    totalImpuesto.ele("baseImponible").txt(fmt(tax.base));
    totalImpuesto.ele("valor").txt(fmt(tax.value));
  }

  infoFactura.ele("propina").txt(fmt(invoice.tip));
  infoFactura.ele("importeTotal").txt(fmt(invoice.totalAmount));
  infoFactura.ele("moneda").txt(invoice.currency);

  const pagos = infoFactura.ele("pagos");
  const pago = pagos.ele("pago");
  pago.ele("formaPago").txt(invoice.paymentMethod);
  pago.ele("total").txt(fmt(invoice.paymentAmount));
  pago.ele("plazo").txt("0");
  pago.ele("unidadTiempo").txt("dias");

  // ── detalles ────────────────────────────────────────────────────
  const detalles = root.ele("detalles");
  for (const item of invoice.items) {
    const detalle = detalles.ele("detalle");
    if (item.mainCode) detalle.ele("codigoPrincipal").txt(item.mainCode);
    if (item.auxCode) detalle.ele("codigoAuxiliar").txt(item.auxCode);
    detalle.ele("descripcion").txt(item.description);
    detalle.ele("cantidad").txt(fmt(item.quantity, 6));
    detalle.ele("precioUnitario").txt(fmt(item.unitPrice, 6));
    detalle.ele("descuento").txt(fmt(item.discount));
    detalle.ele("precioTotalSinImpuesto").txt(fmt(item.subtotalNoTax));

    const impuestos = detalle.ele("impuestos");
    const impuesto = impuestos.ele("impuesto");
    impuesto.ele("codigo").txt(item.taxCode);
    impuesto.ele("codigoPorcentaje").txt(item.taxPercentageCode);
    impuesto.ele("tarifa").txt(fmt(item.taxRate));
    impuesto.ele("baseImponible").txt(fmt(item.taxBase));
    impuesto.ele("valor").txt(fmt(item.taxValue));
  }

  // ── infoAdicional ───────────────────────────────────────────────
  const infoAdicional = root.ele("infoAdicional");
  if (invoice.buyerEmail) {
    infoAdicional.ele("campoAdicional", { nombre: "email" }).txt(invoice.buyerEmail);
  }
  if (invoice.buyerAddress) {
    infoAdicional.ele("campoAdicional", { nombre: "direccion" }).txt(invoice.buyerAddress);
  }

  return root.end({ prettyPrint: false });
}

interface TaxGroup {
  code: string;
  percentageCode: string;
  base: Decimal;
  value: Decimal;
}

function groupTaxes(items: InvoiceItem[]): TaxGroup[] {
  const map = new Map<string, TaxGroup>();
  for (const item of items) {
    const key = `${item.taxCode}-${item.taxPercentageCode}`;
    const existing = map.get(key);
    if (existing) {
      existing.base = existing.base.add(new Decimal(item.taxBase.toString()));
      existing.value = existing.value.add(new Decimal(item.taxValue.toString()));
    } else {
      map.set(key, {
        code: item.taxCode,
        percentageCode: item.taxPercentageCode,
        base: new Decimal(item.taxBase.toString()),
        value: new Decimal(item.taxValue.toString()),
      });
    }
  }
  return Array.from(map.values());
}
