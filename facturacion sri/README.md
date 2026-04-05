# CertificateService (.NET 8)

Microservicio productivo para gestión segura de certificados digitales `.p12` en un entorno multi-tenant.

## Estructura (Clean Architecture)

- `src/CertificateService.Domain`: Entidades + contratos de dominio.
- `src/CertificateService.Application`: Casos de uso + DTOs + contratos de aplicación.
- `src/CertificateService.Infrastructure`: EF Core PostgreSQL, cifrado AES-GCM, Redis, firma XML, worker de expiración.
- `src/CertificateService.API`: Endpoints, middleware de tenant/correlation-id, manejo global de excepciones, rate limiting.

## Seguridad

- Cifrado AES-256-GCM para `encrypted_p12` y `encrypted_password`.
- Clave desde variable de entorno `CERT_ENCRYPTION_KEY` (base64 de 32 bytes).
- Nunca se loguea contraseña ni contenido del `.p12`.
- Hash SHA-256 de thumbprint para búsquedas y trazabilidad sin exponer dato sensible en claro.

### Generar clave de cifrado

```bash
openssl rand -base64 32
```

## API

Todos los endpoints requieren header `X-Tenant-Id`.

- `POST /certificates` (multipart: `file`, `password`, `name`)
- `GET /certificates?tenant_id=<uuid>`
- `GET /certificates/{id}`
- `POST /certificates/{id}/activate`
- `DELETE /certificates/{id}`
- `POST /certificates/sign-xml`

### Ejemplo firma XML

```http
POST /certificates/sign-xml
Content-Type: application/json
X-Tenant-Id: 11111111-1111-1111-1111-111111111111

{
  "xml": "<Factura Id='comprobante'><Info>demo</Info></Factura>"
}
```

## Método interno para firma

Implementado en `ICertificateSigningService.ObtenerCertificadoParaFirmaAsync(tenantId)`:

1. Busca certificado activo del tenant.
2. Descifra p12 y password.
3. Valida certificado y retorna `X509Certificate2` listo para firmar.

## Observabilidad y resiliencia

- Logs estructurados con Serilog.
- Correlation ID en middleware (`X-Correlation-Id`).
- Exportación a Loki para dashboards en Grafana.
- Rate limiting por tenant (30 req/min).
- Manejo global de excepciones con respuestas JSON normalizadas.

## Base de datos

Tabla `certificates`:

- `id`, `tenant_id`, `name`, `encrypted_p12`, `encrypted_password`,
- `thumbprint`, `thumbprint_hash`, `expiration_date`, `is_active`, `created_at`

Índices:

- `tenant_id`
- `is_active`
- `(tenant_id, thumbprint_hash)`

## Docker

```bash
docker compose up --build
```

Servicios incluidos: API, PostgreSQL, Redis, Loki y Grafana.
