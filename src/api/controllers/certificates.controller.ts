import { Request, Response, NextFunction } from "express";
import multer from "multer";
import {
  uploadCertificate,
  listCertificates,
  setCertificateActive,
} from "../../services/certificate.service";

// Multer: almacenar P12 en memoria (nunca en disco)
export const p12Upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 512 * 1024 }, // 512 KB máximo
  fileFilter: (_req, file, cb) => {
    if (
      file.mimetype === "application/x-pkcs12" ||
      file.originalname.endsWith(".p12") ||
      file.originalname.endsWith(".pfx")
    ) {
      cb(null, true);
    } else {
      cb(new Error("Solo se aceptan archivos .p12 o .pfx"));
    }
  },
}).single("certificate");

// ─── GET /certificates ─────────────────────────────────────────────
export async function listCertificatesHandler(
  _req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const certs = await listCertificates();
    res.json({ success: true, certificates: certs });
  } catch (err) {
    next(err);
  }
}

// ─── POST /certificates ────────────────────────────────────────────
// Body: multipart/form-data
//   certificate: <archivo .p12>
//   password: <contraseña del P12>
//   name: <nombre descriptivo>
export async function uploadCertificateHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    if (!req.file) {
      res.status(400).json({ success: false, error: "Se requiere el archivo P12" });
      return;
    }

    const { name, password } = req.body as { name?: string; password?: string };

    if (!password) {
      res.status(400).json({ success: false, error: "Se requiere la contraseña del P12" });
      return;
    }

    const cert = await uploadCertificate({
      name: name ?? req.file.originalname,
      p12Buffer: req.file.buffer,
      password,
    });

    // No retornar el P12 cifrado en la respuesta
    res.status(201).json({
      success: true,
      certificate: {
        id: cert.id,
        name: cert.name,
        ruc: cert.ruc,
        validFrom: cert.validFrom,
        validUntil: cert.validUntil,
        active: cert.active,
      },
    });
  } catch (err) {
    next(err);
  }
}

// ─── PATCH /certificates/:id/activate ────────────────────────────
export async function activateCertificateHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const cert = await setCertificateActive(req.params.id, true);
    res.json({ success: true, certificate: { id: cert.id, active: cert.active } });
  } catch (err) {
    next(err);
  }
}

// ─── PATCH /certificates/:id/deactivate ──────────────────────────
export async function deactivateCertificateHandler(
  req: Request,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const cert = await setCertificateActive(req.params.id, false);
    res.json({ success: true, certificate: { id: cert.id, active: cert.active } });
  } catch (err) {
    next(err);
  }
}
