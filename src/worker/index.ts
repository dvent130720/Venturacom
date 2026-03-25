/**
 * Worker de facturación SRI
 *
 * Procesa los jobs de la cola "billing":
 *   - sign-invoice
 *   - send-invoice
 *   - check-authorization
 *
 * Para iniciar: ts-node src/worker/index.ts
 */
import "dotenv/config";
import { Worker, Job } from "bullmq";
import {
  BILLING_QUEUE,
  JobName,
  redisConnection,
  SignJobData,
  SendJobData,
  CheckAuthJobData,
} from "./queues/billing.queue";
import { processSign } from "./processors/sign.processor";
import { processSend } from "./processors/send.processor";
import { processCheckAuth } from "./processors/check-auth.processor";
import { connectDB, disconnectDB } from "../db/prisma";
import { logger } from "../utils/logger";

async function main() {
  await connectDB();
  logger.info("Worker de facturación SRI iniciado");

  const worker = new Worker(
    BILLING_QUEUE,
    async (job: Job) => {
      switch (job.name) {
        case JobName.SIGN:
          return processSign(job as Job<SignJobData>);
        case JobName.SEND:
          return processSend(job as Job<SendJobData>);
        case JobName.CHECK_AUTH:
          return processCheckAuth(job as Job<CheckAuthJobData>);
        default:
          throw new Error(`Job desconocido: ${job.name}`);
      }
    },
    {
      connection: redisConnection,
      concurrency: 5,
    }
  );

  worker.on("completed", (job) => {
    logger.info(`Job ${job.name} completado`, { jobId: job.id });
  });

  worker.on("failed", (job, err) => {
    logger.error(`Job ${job?.name ?? "??"} falló`, {
      jobId: job?.id,
      error: err.message,
      stack: err.stack,
    });
  });

  worker.on("error", (err) => {
    logger.error("Error en el worker", { error: err.message });
  });

  // Graceful shutdown
  const shutdown = async (signal: string) => {
    logger.info(`Señal ${signal} recibida. Cerrando worker...`);
    await worker.close();
    await disconnectDB();
    process.exit(0);
  };

  process.on("SIGTERM", () => shutdown("SIGTERM"));
  process.on("SIGINT", () => shutdown("SIGINT"));

  logger.info(`Worker escuchando cola "${BILLING_QUEUE}"`, {
    concurrency: 5,
    redis: `${redisConnection.host}:${redisConnection.port}`,
  });
}

main().catch((err) => {
  logger.error("Error fatal en el worker", { error: err });
  process.exit(1);
});
