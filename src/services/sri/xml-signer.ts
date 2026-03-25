/**
 * Firma XAdES-BES para comprobantes electrónicos SRI Ecuador.
 *
 * Proceso:
 *  1. Parsear el P12 con node-forge → extraer clave privada + certificado
 *  2. Convertir a WebCrypto CryptoKey (via @peculiar/webcrypto)
 *  3. Firmar con xadesjs (XAdES-BES, referencia al elemento id="comprobante")
 *  4. Retornar XML con firma embebida
 */
import * as forge from "node-forge";
import { Crypto } from "@peculiar/webcrypto";
import { X509Certificate } from "@peculiar/x509";
import * as XAdES from "xadesjs";
import { logger } from "../../utils/logger";

// ─── Setup global DOM para xadesjs en Node.js ────────────────────
// eslint-disable-next-line @typescript-eslint/no-var-requires
const { DOMParser, XMLSerializer, DOMImplementation } = require("@xmldom/xmldom");
// @ts-ignore
global.DOMParser = DOMParser;
// @ts-ignore
global.XMLSerializer = XMLSerializer;
// @ts-ignore
global.DOMImplementation = DOMImplementation;

const webcrypto = new Crypto();
XAdES.Application.setEngine("OpenSSL", webcrypto);

// ─── Tipos ────────────────────────────────────────────────────────
interface SigningMaterial {
  privateKey: CryptoKey;
  x509: X509Certificate;
}

// ─── Conversión P12 → material de firma ──────────────────────────
export async function loadSigningMaterial(
  p12Buffer: Buffer,
  password: string
): Promise<SigningMaterial> {
  // 1. Parsear P12 con node-forge
  const p12Der = forge.util.createBuffer(p12Buffer.toString("binary"));
  const p12Asn1 = forge.asn1.fromDer(p12Der);
  const p12 = forge.pkcs12.pkcs12FromAsn1(p12Asn1, false, password);

  // 2. Extraer clave privada
  const keyBags = p12.getBags({ bagType: forge.pki.oids.pkcs8ShroudedKeyBag });
  const certBags = p12.getBags({ bagType: forge.pki.oids.certBag });

  const keyBag = keyBags[forge.pki.oids.pkcs8ShroudedKeyBag]?.[0];
  const certBag = certBags[forge.pki.oids.certBag]?.[0];

  if (!keyBag?.key) {
    throw new Error("No se encontró la clave privada en el certificado P12");
  }
  if (!certBag?.cert) {
    throw new Error("No se encontró el certificado en el P12");
  }

  // 3. Convertir clave privada a PKCS#8 DER para WebCrypto
  const privateKeyInfo = forge.pki.wrapRsaPrivateKey(
    forge.pki.privateKeyToAsn1(keyBag.key)
  );
  const pkcs8Der = Buffer.from(
    forge.asn1.toDer(privateKeyInfo).getBytes(),
    "binary"
  );

  // 4. Importar en WebCrypto
  const privateKey = await webcrypto.subtle.importKey(
    "pkcs8",
    pkcs8Der,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-1" },
    false,
    ["sign"]
  );

  // 5. Convertir certificado a DER → X509Certificate
  const certDer = Buffer.from(
    forge.asn1.toDer(forge.pki.certificateToAsn1(certBag.cert)).getBytes(),
    "binary"
  );
  const x509 = new X509Certificate(certDer);

  return { privateKey, x509 };
}

// ─── Firma XAdES-BES ─────────────────────────────────────────────
export async function signXml(
  xmlString: string,
  material: SigningMaterial
): Promise<string> {
  const { privateKey, x509 } = material;

  // Parsear el XML
  const xmlDoc = new DOMParser().parseFromString(xmlString, "application/xml");

  const signedXml = new XAdES.SignedXml();

  await signedXml.Sign(
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-1" },
    privateKey,
    xmlDoc,
    {
      references: [
        {
          id: "r-id-1",
          uri: "#comprobante",
          transforms: ["enveloped", "c14n"],
          digestMethod: "SHA-1",
        },
      ],
      x509: [x509.rawData],
      signingCertificate: x509,
      signerRole: {
        claimed: ["emisor"],
      },
    }
  );

  // Agregar la firma al documento raíz
  const signatureNode = signedXml.GetXml();
  if (!signatureNode) {
    throw new Error("No se pudo obtener el nodo de firma XML");
  }
  xmlDoc.documentElement.appendChild(signatureNode);

  const signedString = new XMLSerializer().serializeToString(xmlDoc);
  logger.debug("XML firmado generado correctamente");
  return signedString;
}

// ─── Función de alto nivel: carga P12 y firma ────────────────────
export async function signInvoiceXml(
  xmlString: string,
  p12Buffer: Buffer,
  p12Password: string
): Promise<string> {
  const material = await loadSigningMaterial(p12Buffer, p12Password);
  return signXml(xmlString, material);
}
