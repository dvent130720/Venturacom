import { Request, Response, NextFunction } from "express";
import { z } from "zod";
import { InvoiceStatus } from "@prisma/client";
import {
  createInvoice,
  getInvoice,
  listInvoices,
  cancelInvoice,
  CreateInvoiceSchema,
} from "../../services/invoice.service";
import {
  enqueueSign,
  enqueueSend,
  enqueueCheckAuth,
} from "../queues/billing.queue.proxy";
import { logger } from "../../utils/logger";

// ─── GET /invoices ─────────────────────────────────────────────────
export async function listInvoicesHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const status = req.query.status as InvoiceStatus | undefined;
    const page = Number(req.query.page ?? 1);
    const limit = Number(req.query.limit ?? 20);

    const result = await listInvoices({ status, page, limit });
    res.json({ success: true, ...result });
  } catch (err) {
    next(err);
  }
}

// ─── POST /invoices ────────────────────────────────────────────────
export async function createInvoiceHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const input = CreateInvoiceSchema.parse(req.body);
    const invoice = await createInvoice(input);

    // Encolar firma automáticamente si hay certificado asignado
    if (invoice.certificateId) {
      await enqueueSign(invoice.id);
      logger.info("Job de firma encolado", { invoiceId: invoice.id });
    }

    res.status(201).json({
      success: true,
      invoice,
      message: invoice.certificateId
        ? "Factura creada. Firma y envío encolados automáticamente."
        : "Factura creada. Asigne un certificado y llame a POST /invoices/:id/sign.",
    });
  } catch (err) {
    next(err);
  }
}

// ─── GET /invoices/:id ────────────────────────────────────────────
export async function getInvoiceHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const invoice = await getInvoice(req.params.id);
    res.json({ success: true, invoice });
  } catch (err) {
    next(err);
  }
}

// ─── POST /invoices/:id/sign ──────────────────────────────────────
export async function signInvoiceHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const job = await enqueueSign(req.params.id);
    res.json({
      success: true,
      message: "Job de firma encolado",
      jobId: job.id,
    });
  } catch (err) {
    next(err);
  }
}

// ─── POST /invoices/:id/send ──────────────────────────────────────
export async function sendInvoiceHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const job = await enqueueSend(req.params.id);
    res.json({
      success: true,
      message: "Job de envío al SRI encolado",
      jobId: job.id,
    });
  } catch (err) {
    next(err);
  }
}

// ─── POST /invoices/:id/check-auth ───────────────────────────────
export async function checkAuthHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const job = await enqueueCheckAuth(req.params.id, 0);
    res.json({
      success: true,
      message: "Consulta de autorización encolada",
      jobId: job.id,
    });
  } catch (err) {
    next(err);
  }
}

// ─── DELETE /invoices/:id ─────────────────────────────────────────
export async function cancelInvoiceHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const { reason } = z.object({ reason: z.string().optional() }).parse(req.body);
    const invoice = await cancelInvoice(req.params.id, reason);
    res.json({ success: true, invoice });
  } catch (err) {
    next(err);
  }
}
