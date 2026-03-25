import { Router } from "express";
import { prisma } from "../../db/prisma";
import { billingQueue } from "../queues/billing.queue.proxy";

export const healthRouter = Router();

healthRouter.get("/", async (_req, res) => {
  try {
    // Verificar DB
    await prisma.$queryRaw`SELECT 1`;

    // Verificar Redis/Queue
    const queueCounts = await billingQueue.getJobCounts(
      "active",
      "waiting",
      "failed"
    );

    res.json({
      status: "ok",
      database: "connected",
      queue: {
        name: billingQueue.name,
        ...queueCounts,
      },
      timestamp: new Date().toISOString(),
    });
  } catch (error) {
    res.status(503).json({
      status: "error",
      error: error instanceof Error ? error.message : String(error),
    });
  }
});
