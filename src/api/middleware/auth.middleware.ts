import { Request, Response, NextFunction } from "express";
import { config } from "../../config";

/**
 * Middleware de autenticación por API Key.
 * La clave se envía en el header: X-API-Key: <key>
 */
export function apiKeyAuth(req: Request, res: Response, next: NextFunction): void {
  const apiKey = req.headers["x-api-key"];

  if (!apiKey || apiKey !== config.API_KEY) {
    res.status(401).json({
      success: false,
      error: "No autorizado. Incluya un header X-API-Key válido.",
    });
    return;
  }

  next();
}
