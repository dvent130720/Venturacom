# Venturacom — Worker de Facturación Electrónica SRI Ecuador

Sistema de facturación electrónica para el **SRI Ecuador** construido con **.NET 8** y **ASP.NET Core**.

## Stack tecnológico

| Componente | Tecnología |
|---|---|
| API REST | ASP.NET Core 8 Web API |
| Worker / Jobs | Hangfire + PostgreSQL |
| Base de datos | PostgreSQL + EF Core 8 (Npgsql) |
| Firma digital | XAdES-BES (`System.Security.Cryptography.Xml`) |
| Comunicación SRI | SOAP via `HttpClient` |
| Cifrado certificados | AES-256-GCM (`System.Security.Cryptography.AesGcm`) |
| Logging | Serilog |

## Arquitectura

```
POST /api/v1/invoices
        │
        ▼
   [DRAFT en DB]
        │
        ▼ Hangfire Job: SignInvoiceJob
   Firma XAdES-BES (certificado P12) → SIGNED
        │
        ▼ Hangfire Job: SendInvoiceJob
   SOAP → celcer.sri.gob.ec/RecepcionComprobantesOffline → SENT
        │
        ▼ Hangfire Job: CheckAuthorizationJob (con delay + reintentos)
   SOAP → celcer.sri.gob.ec/AutorizacionComprobantesOffline
        │
   AUTHORIZED / REJECTED
```

> **Regla de negocio:** Es imposible enviar una factura al SRI sin que el XML
> esté firmado con un certificado P12 válido y vigente.
> El servicio lanza error `422` si se intenta enviar una factura no firmada.

## Inicio rápido

### 1. Configurar variables de entorno

```bash
cp .env.example .env
```

Generar claves seguras:

```bash
# API Key
openssl rand -hex 32

# Encryption Key (32 bytes = 64 hex chars)
openssl rand -hex 32
```

Editar `src/VenturacomSri.Api/appsettings.Development.json` con los valores locales.

### 2. Levantar PostgreSQL (Docker)

```bash
docker-compose up -d postgres
```

### 3. Aplicar migraciones EF Core

```bash
cd src/VenturacomSri.Api
dotnet ef database update
```

O dejar que la app las aplique automáticamente al iniciar.

### 4. Ejecutar la API (incluye el worker Hangfire)

```bash
dotnet run --project src/VenturacomSri.Api
```

La API estará en `http://localhost:5000`.
El dashboard de Hangfire (solo Development) en `http://localhost:5000/hangfire`.

### 5. Docker Compose completo

```bash
docker-compose up --build
```

## API Endpoints

### Health

```
GET /health
```

### Certificados (autenticación: X-API-Key)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/v1/certificates` | Listar certificados |
| POST | `/api/v1/certificates` | Subir certificado P12 (multipart/form-data) |
| PATCH | `/api/v1/certificates/{id}/activate` | Activar |
| PATCH | `/api/v1/certificates/{id}/deactivate` | Desactivar |

```bash
# Subir certificado P12
curl -X POST http://localhost:5000/api/v1/certificates \
  -H "X-API-Key: dev-api-key-change-in-production" \
  -F "certificate=@mi_certificado.p12" \
  -F "password=clave_p12" \
  -F "name=Certificado 2024"
```

### Facturas

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/v1/invoices` | Listar facturas |
| POST | `/api/v1/invoices` | Crear factura (inicia flujo automático) |
| GET | `/api/v1/invoices/{id}` | Consultar factura |
| POST | `/api/v1/invoices/{id}/sign` | Encolar firma manual |
| POST | `/api/v1/invoices/{id}/send` | Encolar envío manual al SRI |
| POST | `/api/v1/invoices/{id}/check-auth` | Consultar autorización |
| DELETE | `/api/v1/invoices/{id}` | Cancelar factura |

```bash
# Crear factura
curl -X POST http://localhost:5000/api/v1/invoices \
  -H "X-API-Key: dev-api-key-change-in-production" \
  -H "Content-Type: application/json" \
  -d '{
    "buyerIdType": "05",
    "buyerId": "1712345678",
    "buyerName": "Juan Pérez",
    "buyerEmail": "juan@example.com",
    "paymentMethod": "01",
    "items": [
      {
        "mainCode": "PROD001",
        "description": "Producto de ejemplo",
        "quantity": 2,
        "unitPrice": 50.00,
        "taxPercentageCode": "2"
      }
    ]
  }'
```

## Flujo de estados

```
DRAFT → SIGNED → SENT → AUTHORIZED
                      ↘ REJECTED
DRAFT → CANCELLED
SIGNED → CANCELLED
```

## Seguridad

| Punto | Mecanismo |
|---|---|
| Certificados P12 en BD | Cifrado **AES-256-GCM** (clave en variables de entorno) |
| Firma digital | **XAdES-BES** con RSA-SHA1 sobre certificado P12 |
| Envío al SRI | Bloqueado si el XML no está firmado (`422 Unprocessable Entity`) |
| API | Header **X-API-Key** en todas las rutas protegidas |
| P12 en memoria | `EphemeralKeySet` — la clave privada no persiste en disco |

## Ambiente SRI

| Parámetro | Valor Pruebas |
|---|---|
| `Sri:Environment` | `1` |
| Recepción | `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline` |
| Autorización | `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline` |
