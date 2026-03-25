/**
 * Cliente SOAP para comunicación con el SRI Ecuador.
 *
 * Endpoints Ambiente de PRUEBAS:
 *   Recepción:    https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline
 *   Autorización: https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline
 *
 * Flujo:
 *   1. validarComprobante(signedXmlBase64)  → RECIBIDA | DEVUELTA
 *   2. autorizacionComprobante(accessKey)   → AUTORIZADO | NO AUTORIZADO
 */
import axios from "axios";
import { config } from "../../config";
import { logger } from "../../utils/logger";

// ─── Tipos de respuesta SRI ───────────────────────────────────────
export type ReceptionState = "RECIBIDA" | "DEVUELTA";

export interface ReceptionResponse {
  state: ReceptionState;
  messages: SriMessage[];
}

export interface AuthorizationResponse {
  authNumber: string;
  authDate: string;
  environment: string;
  state: "AUTORIZADO" | "NO AUTORIZADO";
  comprobante: string;           // XML del comprobante autorizado
  messages: SriMessage[];
}

export interface SriMessage {
  identifier: string;
  message: string;
  additionalInfo?: string;
  type: "ERROR" | "ADVERTENCIA" | "INFORMATIVO";
}

// ─── Soap helpers ────────────────────────────────────────────────
function buildReceptionEnvelope(base64Xml: string): string {
  return `<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope
  xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
  xmlns:ec="http://ec.gob.sri.ws.recepcion">
  <soapenv:Header/>
  <soapenv:Body>
    <ec:validarComprobante>
      <xml>${base64Xml}</xml>
    </ec:validarComprobante>
  </soapenv:Body>
</soapenv:Envelope>`;
}

function buildAuthorizationEnvelope(accessKey: string): string {
  return `<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope
  xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
  xmlns:ec="http://ec.gob.sri.ws.autorizacion">
  <soapenv:Header/>
  <soapenv:Body>
    <ec:autorizacionComprobante>
      <claveAccesoComprobante>${accessKey}</claveAccesoComprobante>
    </ec:autorizacionComprobante>
  </soapenv:Body>
</soapenv:Envelope>`;
}

// ─── Parseo de respuesta XML SRI ─────────────────────────────────
// eslint-disable-next-line @typescript-eslint/no-var-requires
const { DOMParser } = require("@xmldom/xmldom");

function parseMessages(root: Document, tagName: string): SriMessage[] {
  const msgs: SriMessage[] = [];
  const mensajesNode = root.getElementsByTagName(tagName);
  for (let i = 0; i < mensajesNode.length; i++) {
    const m = mensajesNode[i];
    msgs.push({
      identifier: getText(m, "identificador"),
      message: getText(m, "mensaje"),
      additionalInfo: getText(m, "informacionAdicional") || undefined,
      type: (getText(m, "tipo") as SriMessage["type"]) || "ERROR",
    });
  }
  return msgs;
}

function getText(parent: Element, tag: string): string {
  return parent.getElementsByTagName(tag)[0]?.textContent?.trim() ?? "";
}

function parseReceptionResponse(soapXml: string): ReceptionResponse {
  const doc = new DOMParser().parseFromString(soapXml, "text/xml");
  const stateNode = doc.getElementsByTagName("estado")[0];
  const state = (stateNode?.textContent?.trim() ?? "DEVUELTA") as ReceptionState;
  const messages = parseMessages(doc as unknown as Document, "mensaje");
  return { state, messages };
}

function parseAuthorizationResponse(soapXml: string): AuthorizationResponse {
  const doc = new DOMParser().parseFromString(soapXml, "text/xml");

  const authNum = doc.getElementsByTagName("numeroAutorizacion")[0]?.textContent?.trim() ?? "";
  const authDate = doc.getElementsByTagName("fechaAutorizacion")[0]?.textContent?.trim() ?? "";
  const environment = doc.getElementsByTagName("ambiente")[0]?.textContent?.trim() ?? "";
  const state = (doc.getElementsByTagName("estado")[0]?.textContent?.trim() as "AUTORIZADO" | "NO AUTORIZADO") ?? "NO AUTORIZADO";

  // El comprobante autorizado está dentro de <comprobante>
  const comprobante = doc.getElementsByTagName("comprobante")[0]?.textContent?.trim() ?? "";
  const messages = parseMessages(doc as unknown as Document, "mensaje");

  return { authNumber: authNum, authDate, environment, state, comprobante, messages };
}

// ─── Cliente SRI ─────────────────────────────────────────────────
const sriAxios = axios.create({
  timeout: 60_000,
  headers: { "Content-Type": "text/xml; charset=utf-8" },
});

export async function sendToReception(signedXml: string): Promise<ReceptionResponse> {
  const base64 = Buffer.from(signedXml, "utf8").toString("base64");
  const envelope = buildReceptionEnvelope(base64);

  logger.debug("Enviando comprobante a recepción SRI", {
    url: config.SRI_RECEPTION_URL,
  });

  try {
    const response = await sriAxios.post(config.SRI_RECEPTION_URL, envelope, {
      headers: { SOAPAction: "" },
    });
    return parseReceptionResponse(response.data as string);
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : String(error);
    logger.error("Error enviando a recepción SRI", { error: msg });
    throw new Error(`Error de comunicación con SRI (recepción): ${msg}`);
  }
}

export async function checkAuthorization(accessKey: string): Promise<AuthorizationResponse> {
  const envelope = buildAuthorizationEnvelope(accessKey);

  logger.debug("Consultando autorización SRI", {
    accessKey,
    url: config.SRI_AUTHORIZATION_URL,
  });

  try {
    const response = await sriAxios.post(config.SRI_AUTHORIZATION_URL, envelope, {
      headers: { SOAPAction: "" },
    });
    return parseAuthorizationResponse(response.data as string);
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : String(error);
    logger.error("Error consultando autorización SRI", { error: msg });
    throw new Error(`Error de comunicación con SRI (autorización): ${msg}`);
  }
}
