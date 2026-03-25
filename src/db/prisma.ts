import { PrismaClient } from "@prisma/client";
import { logger } from "../utils/logger";

declare global {
  // eslint-disable-next-line no-var
  var __prisma: PrismaClient | undefined;
}

export const prisma: PrismaClient =
  global.__prisma ??
  new PrismaClient({
    log: [
      { emit: "event", level: "query" },
      { emit: "event", level: "error" },
      { emit: "event", level: "warn" },
    ],
  });

if (process.env.NODE_ENV !== "production") {
  global.__prisma = prisma;
}

prisma.$on("error", (e) => logger.error("Prisma error", { message: e.message }));
prisma.$on("warn", (e) => logger.warn("Prisma warn", { message: e.message }));

export async function connectDB(): Promise<void> {
  await prisma.$connect();
  logger.info("Conexión a base de datos establecida");
}

export async function disconnectDB(): Promise<void> {
  await prisma.$disconnect();
  logger.info("Conexión a base de datos cerrada");
}
