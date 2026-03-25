/**
 * Generación de Clave de Acceso SRI Ecuador (49 dígitos)
 *
 * Estructura:
 *   [0..7]   fechaEmision  8 dígitos  ddmmaaaa
 *   [8..9]   tipoComprobante 2 dígitos  01=factura
 *   [10..22] ruc           13 dígitos
 *   [23]     ambiente      1 dígito   1=pruebas, 2=producción
 *   [24..29] serie         6 dígitos  estab(3) + ptoEmi(3)
 *   [30..38] secuencial    9 dígitos
 *   [39..46] codigoNumerico 8 dígitos (aleatorio)
 *   [47]     tipoEmision   1 dígito   1=normal
 *   [48]     digitoVerificador (módulo 11)
 */

export interface AccessKeyParams {
  issueDate: Date;
  documentType?: string; // default "01" (factura)
  ruc: string;
  environment: number;  // 1=pruebas, 2=produccion
  establishment: string;
  emissionPoint: string;
  sequential: string;
  numericCode?: string;
}

function pad(value: string | number, length: number): string {
  return String(value).padStart(length, "0");
}

function mod11CheckDigit(digits: string): number {
  const weights = [2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7, 2, 3, 4, 5, 6, 7];
  let sum = 0;
  for (let i = digits.length - 1; i >= 0; i--) {
    const weight = weights[digits.length - 1 - i] ?? 2;
    sum += parseInt(digits[i], 10) * weight;
  }
  const remainder = sum % 11;
  if (remainder === 0) return 0;
  if (remainder === 1) return 1;
  return 11 - remainder;
}

export function generateAccessKey(params: AccessKeyParams): string {
  const d = params.issueDate;
  const dd = pad(d.getDate(), 2);
  const mm = pad(d.getMonth() + 1, 2);
  const yyyy = String(d.getFullYear());

  const fecha = `${dd}${mm}${yyyy}`;                        // 8
  const tipoCom = params.documentType ?? "01";               // 2
  const ruc = pad(params.ruc, 13);                           // 13
  const ambiente = String(params.environment);               // 1
  const serie = pad(params.establishment, 3) + pad(params.emissionPoint, 3); // 6
  const seq = pad(params.sequential, 9);                     // 9
  const numCode = params.numericCode
    ? pad(params.numericCode, 8)
    : pad(Math.floor(Math.random() * 99999999), 8);          // 8
  const tipoEmision = "1";                                   // 1

  const base = `${fecha}${tipoCom}${ruc}${ambiente}${serie}${seq}${numCode}${tipoEmision}`;
  // base = 48 dígitos
  const checkDigit = mod11CheckDigit(base);

  return `${base}${checkDigit}`;
}

export function formatInvoiceNumber(
  establishment: string,
  emissionPoint: string,
  sequential: string | number
): string {
  return `${pad(establishment, 3)}-${pad(emissionPoint, 3)}-${pad(sequential, 9)}`;
}
