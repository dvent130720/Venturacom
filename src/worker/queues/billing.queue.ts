/**
 * Definición de la cola de facturación y tipos de jobs.
 *
 * Jobs disponibles:
 *   sign-invoice        → Firma el XML con XAdES-BES
 *   send-invoice        → Envía el XML firmado al SRI (recepción)
 *   check-authorization → Consulta el estado de autorización en SRI
 */
import { Queue, QueueEvents } from "bullmq";
import { config } from "../../config";
import { logger } from "../../utils/logger";

// ─── Conexión Redis ────────────────────────────────────────────────
export const redisConnection = {
  host: config.REDIS_HOST,
  port: config.REDIS_PORT,
  password: config.REDIS_PASSWORD,
};

// ─── Nombres de colas y jobs ──────────────────────────────────────
export const BILLING_QUEUE = "billing";

export const JobName = {
  SIGN: "sign-invoice",
  SEND: "send-invoice",
  CHECK_AUTH: "check-authorization",
} as const;

export type JobNameType = (typeof JobName)[keyof typeof JobName];

// ─── Datos de los jobs ────────────────────────────────────────────
export interface SignJobData {
  invoiceId: string;
}
export interface SendJobData {
  invoiceId: string;
}
export interface CheckAuthJobData {
  invoiceId: string;
  attempt?: number;
}

// ─── Instancia de la cola ─────────────────────────────────────────
export const billingQueue = new Queue(BILLING_QUEUE, {
  connection: redisConnection,
  defaultJobOptions: {
    attempts: config.WORKER_MAX_RETRIES,
    backoff: {
      type: "exponential",
      delay: config.WORKER_RETRY_DELAY_MS,
    },
    removeOnComplete: { count: 500 },
    removeOnFail: { count: 200 },
  },
});

// ─── Helpers para encolar jobs ────────────────────────────────────
export async function enqueueSign(invoiceId: string) {
  const job = await billingQueue.add(JobName.SIGN, { invoiceId } as SignJobData, {
    jobId: `sign-${invoiceId}`,
  });
  logger.debug("Job sign encolado", { jobId: job.id, invoiceId });
  return job;
}

export async function enqueueSend(invoiceId: string) {
  const job = await billingQueue.add(JobName.SEND, { invoiceId } as SendJobData, {
    jobId: `send-${invoiceId}`,
  });
  logger.debug("Job send encolado", { jobId: job.id, invoiceId });
  return job;
}

export async function enqueueCheckAuth(invoiceId: string, delayMs = 5_000) {
  const job = await billingQueue.add(
    JobName.CHECK_AUTH,
    { invoiceId, attempt: 1 } as CheckAuthJobData,
    {
      jobId: `check-auth-${invoiceId}-${Date.now()}`,
      delay: delayMs,
    }
  );
  logger.debug("Job check-auth encolado", { jobId: job.id, invoiceId, delayMs });
  return job;
}

// ─── Eventos de la cola (monitoreo) ──────────────────────────────
export const billingQueueEvents = new QueueEvents(BILLING_QUEUE, {
  connection: redisConnection,
});

billingQueueEvents.on("completed", ({ jobId }) => {
  logger.debug("Job completado", { jobId });
});

billingQueueEvents.on("failed", ({ jobId, failedReason }) => {
  logger.error("Job fallido", { jobId, failedReason });
});
