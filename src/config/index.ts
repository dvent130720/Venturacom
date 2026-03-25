import dotenv from "dotenv";
import { z } from "zod";

dotenv.config();

const envSchema = z.object({
  NODE_ENV: z.enum(["development", "production", "test"]).default("development"),
  PORT: z.coerce.number().default(3000),

  DATABASE_URL: z.string().min(1),

  REDIS_HOST: z.string().default("localhost"),
  REDIS_PORT: z.coerce.number().default(6379),
  REDIS_PASSWORD: z.string().optional(),

  API_KEY: z.string().min(16),

  // 32 bytes como hex (64 chars)
  ENCRYPTION_KEY: z.string().length(64),

  SRI_ENVIRONMENT: z.coerce.number().int().min(1).max(2).default(1),
  SRI_RECEPTION_URL: z
    .string()
    .default(
      "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline"
    ),
  SRI_AUTHORIZATION_URL: z
    .string()
    .default(
      "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline"
    ),

  WORKER_MAX_RETRIES: z.coerce.number().default(3),
  WORKER_RETRY_DELAY_MS: z.coerce.number().default(30000),

  ISSUER_RUC: z.string().length(13),
  ISSUER_BUSINESS_NAME: z.string().min(1),
  ISSUER_TRADE_NAME: z.string().optional(),
  ISSUER_ADDRESS: z.string().min(1),
  ISSUER_ESTABLISHMENT: z.string().length(3).default("001"),
  ISSUER_EMISSION_POINT: z.string().length(3).default("001"),
});

const parsed = envSchema.safeParse(process.env);

if (!parsed.success) {
  console.error("❌ Variables de entorno inválidas:");
  console.error(parsed.error.flatten().fieldErrors);
  process.exit(1);
}

export const config = parsed.data;
export type Config = typeof config;
