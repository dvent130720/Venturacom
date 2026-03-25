/**
 * Servicio de facturación.
 *
 * Regla clave: NO se puede enviar al SRI sin firma electrónica.
 * La firma es un paso obligatorio antes del envío.
 *
 * Estados:
 *   DRAFT → (sign) → SIGNED → (send) → SENT → (check) → AUTHORIZED | REJECTED
 */
import Decimal from "decimal.js";
import { InvoiceStatus, Invoice } from "@prisma/client";
import { z } from "zod";
import { prisma } from "../db/prisma";
import { config } from "../config";
import { generateAccessKey, formatInvoiceNumber } from "../utils/access-key";
import { generateInvoiceXml } from "./sri/xml-generator";
import { signInvoiceXml } from "./sri/xml-signer";
import { getDecryptedP12, getActiveCertificateByRuc } from "./certificate.service";
import { sendToReception, checkAuthorization } from "./sri/sri-client";
import { logger } from "../utils/logger";

// ─── Schemas de entrada ───────────────────────────────────────────
export const CreateInvoiceItemSchema = z.object({
  mainCode: z.string().max(25).optional(),
  auxCode: z.string().max(25).optional(),
  description: z.string().min(1).max(300),
  quantity: z.number().positive(),
  unitPrice: z.number().positive(),
  discount: z.number().min(0).default(0),
  taxPercentageCode: z
    .enum(["0", "2", "3", "6", "7", "10"])
    .default("2"), // 0=0%, 2=12%, 10=15%
});

export const CreateInvoiceSchema = z.object({
  issueDate: z.coerce.date().optional(),
  buyerIdType: z.enum(["04", "05", "06", "07"]).default("05"),
  buyerId: z.string().min(1).max(20),
  buyerName: z.string().min(1).max(300),
  buyerEmail: z.string().email().optional(),
  buyerAddress: z.string().max(300).optional(),
  paymentMethod: z
    .enum(["01", "15", "16", "17", "18", "19", "20", "21"])
    .default("01"),
  items: z.array(CreateInvoiceItemSchema).min(1),
  certificateId: z.string().optional(),
});

export type CreateInvoiceInput = z.infer<typeof CreateInvoiceSchema>;

// ─── Tabla de tarifas IVA ─────────────────────────────────────────
const IVA_RATES: Record<string, number> = {
  "0": 0,    // IVA 0%
  "2": 12,   // IVA 12%
  "3": 14,   // IVA 14%
  "6": 0,    // No objeto de impuesto (no suma)
  "7": 0,    // Exento
  "10": 15,  // IVA 15%
};

// ─── Obtener siguiente secuencial ────────────────────────────────
async function getNextSequential(
  establishment: string,
  emissionPoint: string
): Promise<string> {
  const last = await prisma.invoice.findFirst({
    where: { establishment, emissionPoint },
    orderBy: { sequential: "desc" },
    select: { sequential: true },
  });
  const next = last ? parseInt(last.sequential, 10) + 1 : 1;
  return String(next).padStart(9, "0");
}

// ─── Crear factura (DRAFT) ────────────────────────────────────────
export async function createInvoice(input: CreateInvoiceInput): Promise<Invoice> {
  const validated = CreateInvoiceSchema.parse(input);

  const issueDate = validated.issueDate ?? new Date();
  const establishment = config.ISSUER_ESTABLISHMENT;
  const emissionPoint = config.ISSUER_EMISSION_POINT;
  const sequential = await getNextSequential(establishment, emissionPoint);

  const accessKey = generateAccessKey({
    issueDate,
    ruc: config.ISSUER_RUC,
    environment: config.SRI_ENVIRONMENT,
    establishment,
    emissionPoint,
    sequential,
  });

  // Calcular totales por ítem
  let subtotal = new Decimal(0);
  let totalDiscount = new Decimal(0);
  let totalIva = new Decimal(0);

  const itemsData = validated.items.map((item) => {
    const qty = new Decimal(item.quantity);
    const price = new Decimal(item.unitPrice);
    const disc = new Decimal(item.discount);
    const lineSubtotal = qty.mul(price).minus(disc);
    const taxRate = new Decimal(IVA_RATES[item.taxPercentageCode] ?? 0);
    const taxValue = lineSubtotal.mul(taxRate).div(100);

    subtotal = subtotal.add(lineSubtotal);
    totalDiscount = totalDiscount.add(disc);
    totalIva = totalIva.add(taxValue);

    return {
      mainCode: item.mainCode,
      auxCode: item.auxCode,
      description: item.description,
      quantity: qty.toFixed(6),
      unitPrice: price.toFixed(6),
      discount: disc.toFixed(2),
      subtotalNoTax: lineSubtotal.toFixed(2),
      taxCode: "2",
      taxPercentageCode: item.taxPercentageCode,
      taxRate: taxRate.toFixed(2),
      taxBase: lineSubtotal.toFixed(2),
      taxValue: taxValue.toFixed(2),
    };
  });

  const totalAmount = subtotal.add(totalIva);

  // Determinar certificado
  const certId = validated.certificateId ?? (
    await getActiveCertificateByRuc(config.ISSUER_RUC)
  )?.id ?? null;

  const invoice = await prisma.invoice.create({
    data: {
      accessKey,
      status: "DRAFT",
      environment: config.SRI_ENVIRONMENT,
      issuerRuc: config.ISSUER_RUC,
      issuerName: config.ISSUER_BUSINESS_NAME,
      issuerTradeName: config.ISSUER_TRADE_NAME ?? null,
      issuerAddress: config.ISSUER_ADDRESS,
      establishment,
      emissionPoint,
      sequential,
      issueDate,
      buyerIdType: validated.buyerIdType,
      buyerId: validated.buyerId,
      buyerName: validated.buyerName,
      buyerEmail: validated.buyerEmail ?? null,
      buyerAddress: validated.buyerAddress ?? null,
      subtotal: subtotal.toFixed(2),
      totalDiscount: totalDiscount.toFixed(2),
      totalIva: totalIva.toFixed(2),
      totalAmount: totalAmount.toFixed(2),
      tip: "0.00",
      paymentMethod: validated.paymentMethod,
      paymentAmount: totalAmount.toFixed(2),
      certificateId: certId,
      items: { create: itemsData },
    },
    include: { items: true },
  });

  await prisma.auditLog.create({
    data: {
      invoiceId: invoice.id,
      action: "CREATED",
      details: `Factura ${formatInvoiceNumber(establishment, emissionPoint, sequential)} creada`,
    },
  });

  logger.info("Factura creada", { id: invoice.id, accessKey });
  return invoice;
}

// ─── Firmar factura ───────────────────────────────────────────────
export async function signInvoice(invoiceId: string): Promise<Invoice> {
  const invoice = await prisma.invoice.findUniqueOrThrow({
    where: { id: invoiceId },
    include: { items: true },
  });

  if (invoice.status !== "DRAFT") {
    throw new Error(
      `Solo se pueden firmar facturas en estado DRAFT. Estado actual: ${invoice.status}`
    );
  }

  if (!invoice.certificateId) {
    throw new Error(
      "La factura no tiene un certificado asignado. Suba un certificado P12 antes de firmar."
    );
  }

  // Generar XML
  const xmlContent = generateInvoiceXml(invoice as Parameters<typeof generateInvoiceXml>[0]);

  // Obtener P12 descifrado y firmar
  const { p12Buffer, password } = await getDecryptedP12(invoice.certificateId);
  const signedXml = await signInvoiceXml(xmlContent, p12Buffer, password);

  const updated = await prisma.invoice.update({
    where: { id: invoiceId },
    data: {
      status: "SIGNED",
      xmlContent,
      signedXml,
    },
  });

  await prisma.auditLog.create({
    data: {
      invoiceId,
      action: "SIGNED",
      details: "XML firmado con XAdES-BES",
    },
  });

  logger.info("Factura firmada", { id: invoiceId });
  return updated;
}

// ─── Enviar al SRI (solo si está firmada) ────────────────────────
export async function sendInvoiceToSri(invoiceId: string): Promise<Invoice> {
  const invoice = await prisma.invoice.findUniqueOrThrow({
    where: { id: invoiceId },
  });

  if (invoice.status !== "SIGNED") {
    throw new Error(
      `No se puede enviar al SRI una factura sin firma. Estado: ${invoice.status}. ` +
        "Firme primero la factura."
    );
  }

  if (!invoice.signedXml) {
    throw new Error("El XML firmado no está disponible. Firme la factura primero.");
  }

  const reception = await sendToReception(invoice.signedXml);

  let updated: Invoice;

  if (reception.state === "RECIBIDA") {
    updated = await prisma.invoice.update({
      where: { id: invoiceId },
      data: {
        status: "SENT",
        sriResponse: JSON.stringify(reception),
      },
    });
    await prisma.auditLog.create({
      data: {
        invoiceId,
        action: "SENT",
        details: `Recibida por SRI. Mensajes: ${JSON.stringify(reception.messages)}`,
      },
    });
    logger.info("Factura enviada al SRI", { id: invoiceId });
  } else {
    const reason = reception.messages.map((m) => m.message).join("; ");
    updated = await prisma.invoice.update({
      where: { id: invoiceId },
      data: {
        status: "REJECTED",
        sriResponse: JSON.stringify(reception),
        rejectReason: reason,
      },
    });
    await prisma.auditLog.create({
      data: {
        invoiceId,
        action: "REJECTED",
        details: `SRI rechazó la recepción: ${reason}`,
      },
    });
    logger.warn("SRI rechazó la factura en recepción", { id: invoiceId, reason });
  }

  return updated;
}

// ─── Verificar autorización ───────────────────────────────────────
export async function checkInvoiceAuthorization(invoiceId: string): Promise<Invoice> {
  const invoice = await prisma.invoice.findUniqueOrThrow({
    where: { id: invoiceId },
  });

  if (invoice.status !== "SENT") {
    throw new Error(
      `Solo se puede verificar autorización de facturas en estado SENT. Estado: ${invoice.status}`
    );
  }

  const auth = await checkAuthorization(invoice.accessKey);

  let updated: Invoice;

  if (auth.state === "AUTORIZADO") {
    updated = await prisma.invoice.update({
      where: { id: invoiceId },
      data: {
        status: "AUTHORIZED",
        authNumber: auth.authNumber,
        authDate: auth.authDate ? new Date(auth.authDate) : null,
        authXml: auth.comprobante,
      },
    });
    await prisma.auditLog.create({
      data: {
        invoiceId,
        action: "AUTHORIZED",
        details: `Autorizada por SRI. Número: ${auth.authNumber}`,
      },
    });
    logger.info("Factura autorizada por SRI", {
      id: invoiceId,
      authNumber: auth.authNumber,
    });
  } else {
    const reason = auth.messages.map((m) => m.message).join("; ");
    updated = await prisma.invoice.update({
      where: { id: invoiceId },
      data: {
        status: "REJECTED",
        rejectReason: reason,
      },
    });
    await prisma.auditLog.create({
      data: {
        invoiceId,
        action: "REJECTED",
        details: `SRI no autorizó: ${reason}`,
      },
    });
    logger.warn("Factura no autorizada por SRI", { id: invoiceId, reason });
  }

  return updated;
}

// ─── Cancelar factura (solo DRAFT o SIGNED) ───────────────────────
export async function cancelInvoice(invoiceId: string, reason?: string): Promise<Invoice> {
  const invoice = await prisma.invoice.findUniqueOrThrow({
    where: { id: invoiceId },
  });

  if (!["DRAFT", "SIGNED"].includes(invoice.status)) {
    throw new Error(
      `Solo se pueden cancelar facturas en estado DRAFT o SIGNED. Estado: ${invoice.status}`
    );
  }

  const updated = await prisma.invoice.update({
    where: { id: invoiceId },
    data: { status: "CANCELLED" },
  });

  await prisma.auditLog.create({
    data: {
      invoiceId,
      action: "CANCELLED",
      details: reason ?? "Cancelada manualmente",
    },
  });

  return updated;
}

// ─── Consultas ────────────────────────────────────────────────────
export async function getInvoice(id: string) {
  return prisma.invoice.findUniqueOrThrow({
    where: { id },
    include: { items: true, auditLogs: { orderBy: { createdAt: "asc" } } },
  });
}

export async function listInvoices(filters: {
  status?: InvoiceStatus;
  page?: number;
  limit?: number;
}) {
  const page = filters.page ?? 1;
  const limit = Math.min(filters.limit ?? 20, 100);
  const skip = (page - 1) * limit;

  const [total, invoices] = await Promise.all([
    prisma.invoice.count({ where: { status: filters.status } }),
    prisma.invoice.findMany({
      where: { status: filters.status },
      orderBy: { createdAt: "desc" },
      skip,
      take: limit,
      include: { items: true },
    }),
  ]);

  return { total, page, limit, invoices };
}
