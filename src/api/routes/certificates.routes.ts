import { Router } from "express";
import {
  listCertificatesHandler,
  uploadCertificateHandler,
  activateCertificateHandler,
  deactivateCertificateHandler,
  p12Upload,
} from "../controllers/certificates.controller";

export const certificatesRouter = Router();

certificatesRouter.get("/", listCertificatesHandler);
certificatesRouter.post("/", p12Upload, uploadCertificateHandler);
certificatesRouter.patch("/:id/activate", activateCertificateHandler);
certificatesRouter.patch("/:id/deactivate", deactivateCertificateHandler);
