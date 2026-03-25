import { Job } from "bullmq";
import { CheckAuthJobData, enqueueCheckAuth } from "../queues/billing.queue";
import { checkInvoiceAuthorization } from "../../services/invoice.service";
import { logger } from "../../utils/logger";

const MAX_CHECK_ATTEMPTS = 10;
const RETRY_DELAY_MS = 30_000; // 30 segundos entre reintentos

export async function processCheckAuth(job: Job<CheckAuthJobData>): Promise<void> {
  const { invoiceId, attempt = 1 } = job.data;
  logger.info("Verificando autorización SRI", { invoiceId, attempt, jobId: job.id });

  await job.updateProgress(20);
  const invoice = await checkInvoiceAuthorization(invoiceId);
  await job.updateProgress(90);

  if (invoice.status === "AUTHORIZED") {
    logger.info("Factura autorizada", {
      invoiceId,
      authNumber: invoice.authNumber,
    });
  } else if (invoice.status === "SENT" && attempt < MAX_CHECK_ATTEMPTS) {
    // SRI aún no ha procesado; reintentar
    logger.debug("Autorización pendiente, reintentando", {
      invoiceId,
      attempt,
      nextAttempt: attempt + 1,
    });
    await enqueueCheckAuth(invoiceId, RETRY_DELAY_MS);
  } else if (attempt >= MAX_CHECK_ATTEMPTS) {
    logger.error("Máximo de intentos de autorización alcanzado", { invoiceId });
  }

  await job.updateProgress(100);
}
