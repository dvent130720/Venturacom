import { Request, Response, NextFunction } from "express";
import { ZodError } from "zod";
import { Prisma } from "@prisma/client";
import { logger } from "../../utils/logger";

export function errorHandler(
  err: Error,
  _req: Request,
  res: Response,
  _next: NextFunction
): void {
  // Errores de validación Zod
  if (err instanceof ZodError) {
    res.status(400).json({
      success: false,
      error: "Datos inválidos",
      details: err.flatten().fieldErrors,
    });
    return;
  }

  // Registro no encontrado (Prisma)
  if (
    err instanceof Prisma.PrismaClientKnownRequestError &&
    err.code === "P2025"
  ) {
    res.status(404).json({ success: false, error: "Recurso no encontrado" });
    return;
  }

  // Clave única violada (Prisma)
  if (
    err instanceof Prisma.PrismaClientKnownRequestError &&
    err.code === "P2002"
  ) {
    res.status(409).json({ success: false, error: "El recurso ya existe" });
    return;
  }

  // Errores de negocio conocidos
  const knownMessages = [
    "Solo se pueden firmar",
    "No se puede enviar",
    "El certificado",
    "La factura no tiene",
    "Solo se pueden cancelar",
    "Solo se puede verificar",
  ];
  if (knownMessages.some((m) => err.message.includes(m))) {
    res.status(422).json({ success: false, error: err.message });
    return;
  }

  // Error genérico
  logger.error("Error no controlado", { message: err.message, stack: err.stack });
  res.status(500).json({
    success: false,
    error:
      process.env.NODE_ENV === "production"
        ? "Error interno del servidor"
        : err.message,
  });
}

export function notFound(_req: Request, res: Response): void {
  res.status(404).json({ success: false, error: "Ruta no encontrada" });
}
