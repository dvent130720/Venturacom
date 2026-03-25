import { Router } from "express";
import {
  listInvoicesHandler,
  createInvoiceHandler,
  getInvoiceHandler,
  signInvoiceHandler,
  sendInvoiceHandler,
  checkAuthHandler,
  cancelInvoiceHandler,
} from "../controllers/invoices.controller";

export const invoicesRouter = Router();

invoicesRouter.get("/", listInvoicesHandler);
invoicesRouter.post("/", createInvoiceHandler);
invoicesRouter.get("/:id", getInvoiceHandler);

// Acciones de ciclo de vida — requieren firma antes del envío
invoicesRouter.post("/:id/sign", signInvoiceHandler);
invoicesRouter.post("/:id/send", sendInvoiceHandler);
invoicesRouter.post("/:id/check-auth", checkAuthHandler);
invoicesRouter.delete("/:id", cancelInvoiceHandler);
