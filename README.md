# Venturacom — Worker de Facturación Electrónica SRI Ecuador

Sistema de facturación electrónica para el **SRI Ecuador** con API REST y worker asíncrono.

## Características

- Generación de XML de facturas según esquema SRI v2.1.0
- **Firma obligatoria XAdES-BES** antes de enviar al SRI
- Almacenamiento seguro de certificados P12 (cifrado AES-256-GCM)
- Worker asíncrono con BullMQ + Redis (sign → send → authorize)
- Base de datos PostgreSQL con Prisma ORM
- Ambiente de **pruebas** configurado por defecto
- API REST con autenticación por API Key

## Arquitectura

```
POST /api/v1/invoices
        │
        ▼
   [DRAFT en DB]
        │
        ▼ (worker)
   sign-invoice  ←── XAdES-BES con certificado P12
        │
        ▼ (worker)
   send-invoice  ←── SOAP → SRI celcer.sri.gob.ec
        │
        ▼ (worker, con delay)
check-authorization  ←── SOAP → SRI
        │
    AUTORIZADO / RECHAZADO
```

## Inicio rápido

### 1. Configuración

```bash
cp .env.example .env
# Editar .env con sus valores
```

Generar claves:
```bash
# API Key
openssl rand -hex 32

# Encryption Key (32 bytes = 64 hex chars)
openssl rand -hex 32
```

### 2. Levantar servicios (Docker)

```bash
docker-compose up -d
```

### 3. Instalar dependencias y migrar DB

```bash
npm install
npx prisma migrate dev --name init
npx prisma generate
```

### 4. Iniciar API y Worker

```bash
# Terminal 1 — API
npm run dev:api

# Terminal 2 — Worker
npm run dev:worker
```

## API Endpoints

### Certificados

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/v1/certificates` | Listar certificados |
| POST | `/api/v1/certificates` | Subir certificado P12 (multipart/form-data) |
| PATCH | `/api/v1/certificates/:id/activate` | Activar certificado |
| PATCH | `/api/v1/certificates/:id/deactivate` | Desactivar certificado |

**Subir certificado:**
```bash
curl -X POST http://localhost:3000/api/v1/certificates \
  -H "X-API-Key: <tu-api-key>" \
  -F "certificate=@/ruta/al/certificado.p12" \
  -F "password=clave_p12" \
  -F "name=Certificado Empresa 2024"
```

### Facturas

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/v1/invoices` | Listar facturas |
| POST | `/api/v1/invoices` | Crear factura (inicia flujo automático) |
| GET | `/api/v1/invoices/:id` | Consultar factura |
| POST | `/api/v1/invoices/:id/sign` | Encolar firma manual |
| POST | `/api/v1/invoices/:id/send` | Encolar envío manual al SRI |
| POST | `/api/v1/invoices/:id/check-auth` | Consultar autorización |
| DELETE | `/api/v1/invoices/:id` | Cancelar factura |

**Crear factura:**
```bash
curl -X POST http://localhost:3000/api/v1/invoices \
  -H "X-API-Key: <tu-api-key>" \
  -H "Content-Type: application/json" \
  -d '{
    "buyerIdType": "05",
    "buyerId": "1234567890",
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

### Health

```bash
curl http://localhost:3000/health
```

## Flujo de estados

```
DRAFT → SIGNED → SENT → AUTHORIZED
                      ↘ REJECTED
```

> **Importante:** No es posible enviar una factura al SRI sin antes firmarla
> con un certificado P12 válido. El sistema rechaza el envío si el XML no está firmado.

## Ambiente de pruebas SRI

- Recepción: `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline`
- Autorización: `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline`
