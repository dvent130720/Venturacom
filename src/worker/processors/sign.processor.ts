import { Job } from "bullmq";
import { SignJobData } from "../queues/billing.queue";
import { signInvoice } from "../../services/invoice.service";
import { enqueueSend } from "../queues/billing.queue";
import { logger } from "../../utils/logger";

export async function processSign(job: Job<SignJobData>): Promise<void> {
  const { invoiceId } = job.data;
  logger.info("Procesando firma de factura", { invoiceId, jobId: job.id });

  await job.updateProgress(10);
  const invoice = await signInvoice(invoiceId);
  await job.updateProgress(90);

  // Encolar envío automáticamente después de firmar
  await enqueueSend(invoiceId);
  await job.updateProgress(100);

  logger.info("Factura firmada y envío encolado", {
    invoiceId,
    status: invoice.status,
  });
}
