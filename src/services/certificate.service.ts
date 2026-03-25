/**
 * Servicio para gestión de certificados electrónicos P12.
 * Los certificados se almacenan cifrados (AES-256-GCM) en la base de datos.
 * NUNCA se almacena el P12 en texto plano.
 */
import * as forge from "node-forge";
import { Certificate } from "@prisma/client";
import { prisma } from "../db/prisma";
import { encrypt, decrypt } from "../utils/crypto";
import { logger } from "../utils/logger";

export interface UploadCertificateInput {
  name: string;
  p12Buffer: Buffer;
  password: string;
}

export interface CertificateInfo {
  id: string;
  name: string;
  ruc: string;
  validFrom: Date;
  validUntil: Date;
  active: boolean;
  subject: string;
}

// ─── Parsear info del P12 ─────────────────────────────────────────
function extractCertInfo(p12Buffer: Buffer, password: string) {
  const p12Der = forge.util.createBuffer(p12Buffer.toString("binary"));
  const p12Asn1 = forge.asn1.fromDer(p12Der);
  const p12 = forge.pkcs12.pkcs12FromAsn1(p12Asn1, false, password);

  const certBags = p12.getBags({ bagType: forge.pki.oids.certBag });
  const certBag = certBags[forge.pki.oids.certBag]?.[0];

  if (!certBag?.cert) {
    throw new Error("No se encontró certificado en el archivo P12");
  }

  const cert = certBag.cert;
  const subject = cert.subject.attributes
    .map((a) => `${a.shortName}=${a.value}`)
    .join(", ");

  // Extraer RUC del subject (CN o serialNumber)
  const cnAttr = cert.subject.getField("CN");
  const rucAttr = cert.subject.getField("serialNumber");
  const ruc = (rucAttr?.value ?? cnAttr?.value ?? "").replace(/\D/g, "").slice(0, 13);

  return {
    ruc,
    subject,
    validFrom: cert.validity.notBefore,
    validUntil: cert.validity.notAfter,
  };
}

// ─── Subir certificado ────────────────────────────────────────────
export async function uploadCertificate(
  input: UploadCertificateInput
): Promise<Certificate> {
  const info = extractCertInfo(input.p12Buffer, input.password);

  if (info.validUntil < new Date()) {
    throw new Error(
      `El certificado expiró el ${info.validUntil.toISOString()}. No se puede usar.`
    );
  }

  // Cifrar P12 y contraseña
  const p12Base64 = input.p12Buffer.toString("base64");
  const encryptedP12 = encrypt(p12Base64);
  const encryptedPass = encrypt(input.password);

  const cert = await prisma.certificate.create({
    data: {
      name: input.name,
      ruc: info.ruc,
      encryptedP12,
      encryptedPass,
      validFrom: info.validFrom,
      validUntil: info.validUntil,
      active: true,
    },
  });

  logger.info("Certificado subido correctamente", {
    id: cert.id,
    ruc: info.ruc,
    validUntil: info.validUntil.toISOString(),
  });

  return cert;
}

// ─── Obtener material de firma (P12 descifrado) ───────────────────
export async function getDecryptedP12(
  certificateId: string
): Promise<{ p12Buffer: Buffer; password: string }> {
  const cert = await prisma.certificate.findUniqueOrThrow({
    where: { id: certificateId },
  });

  if (!cert.active) {
    throw new Error("El certificado está desactivado");
  }
  if (cert.validUntil < new Date()) {
    throw new Error(
      `El certificado expiró el ${cert.validUntil.toISOString()}`
    );
  }

  const p12Base64 = decrypt(cert.encryptedP12);
  const password = decrypt(cert.encryptedPass);
  const p12Buffer = Buffer.from(p12Base64, "base64");

  return { p12Buffer, password };
}

// ─── Buscar certificado activo por RUC ────────────────────────────
export async function getActiveCertificateByRuc(
  ruc: string
): Promise<Certificate | null> {
  return prisma.certificate.findFirst({
    where: {
      ruc,
      active: true,
      validUntil: { gt: new Date() },
    },
    orderBy: { validUntil: "desc" },
  });
}

// ─── Listar certificados ──────────────────────────────────────────
export async function listCertificates(): Promise<CertificateInfo[]> {
  const certs = await prisma.certificate.findMany({
    orderBy: { createdAt: "desc" },
  });
  return certs.map((c) => ({
    id: c.id,
    name: c.name,
    ruc: c.ruc,
    validFrom: c.validFrom,
    validUntil: c.validUntil,
    active: c.active,
    subject: "",
  }));
}

// ─── Activar / desactivar ─────────────────────────────────────────
export async function setCertificateActive(
  id: string,
  active: boolean
): Promise<Certificate> {
  return prisma.certificate.update({
    where: { id },
    data: { active },
  });
}
