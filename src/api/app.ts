import express from "express";
import { apiKeyAuth } from "./middleware/auth.middleware";
import { errorHandler, notFound } from "./middleware/error.middleware";
import { invoicesRouter } from "./routes/invoices.routes";
import { certificatesRouter } from "./routes/certificates.routes";
import { healthRouter } from "./routes/health.routes";

const app = express();

// ─── Parsers ──────────────────────────────────────────────────────
app.use(express.json({ limit: "2mb" }));
app.use(express.urlencoded({ extended: true }));

// ─── Health (sin autenticación) ───────────────────────────────────
app.use("/health", healthRouter);

// ─── Autenticación ────────────────────────────────────────────────
app.use(apiKeyAuth);

// ─── Rutas protegidas ─────────────────────────────────────────────
app.use("/api/v1/invoices", invoicesRouter);
app.use("/api/v1/certificates", certificatesRouter);

// ─── Error handling ───────────────────────────────────────────────
app.use(notFound);
app.use(errorHandler);

export { app };
