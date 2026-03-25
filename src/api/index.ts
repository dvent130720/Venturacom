/**
 * Servidor API de facturación SRI
 *
 * Para iniciar: ts-node src/api/index.ts
 */
import "dotenv/config";
import { app } from "./app";
import { config } from "../config";
import { connectDB, disconnectDB } from "../db/prisma";
import { logger } from "../utils/logger";

async function main() {
  await connectDB();

  const server = app.listen(config.PORT, () => {
    logger.info(`API de facturación SRI iniciada`, {
      port: config.PORT,
      environment: config.NODE_ENV,
      sri_environment: config.SRI_ENVIRONMENT === 1 ? "PRUEBAS" : "PRODUCCIÓN",
    });
  });

  // Graceful shutdown
  const shutdown = async (signal: string) => {
    logger.info(`Señal ${signal} recibida. Cerrando servidor...`);
    server.close(async () => {
      await disconnectDB();
      process.exit(0);
    });
  };

  process.on("SIGTERM", () => shutdown("SIGTERM"));
  process.on("SIGINT", () => shutdown("SIGINT"));
}

main().catch((err) => {
  logger.error("Error fatal al iniciar la API", { error: err });
  process.exit(1);
});
