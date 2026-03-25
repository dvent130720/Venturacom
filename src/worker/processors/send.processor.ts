import { Job } from "bullmq";
import { SendJobData, enqueueCheckAuth } from "../queues/billing.queue";
import { sendInvoiceToSri } from "../../services/invoice.service";
import { logger } from "../../utils/logger";

// Esperar 10 segundos antes de consultar la autorización
const CHECK_AUTH_DELAY_MS = 10_000;

export async function processSend(job: Job<SendJobData>): Promise<void> {
  const { invoiceId } = job.data;
  logger.info("Procesando envío al SRI", { invoiceId, jobId: job.id });

  await job.updateProgress(20);
  const invoice = await sendInvoiceToSri(invoiceId);
  await job.updateProgress(80);

  if (invoice.status === "SENT") {
    // Encolar verificación de autorización con delay
    await enqueueCheckAuth(invoiceId, CHECK_AUTH_DELAY_MS);
    logger.info("Factura enviada al SRI, autorización encolada", { invoiceId });
  } else {
    logger.warn("SRI devolvió la factura en recepción", {
      invoiceId,
      status: invoice.status,
    });
  }

  await job.updateProgress(100);
}
