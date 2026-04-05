# CertificateService (.NET 8)

Microservicio para gestión de certificados digitales `.p12` en un entorno multi-tenant.

## Estructura (Clean Architecture)

- `src/CertificateService.Domain`: Entidades + contratos de dominio.
- `src/CertificateService.Application`: Casos de uso + DTOs + contratos de aplicación.
- `src/CertificateService.Infrastructure`: EF Core PostgreSQL, cifrado AES-GCM, Redis, firma XML.
- `src/CertificateService.API`: Endpoints, middleware de tenant/correlation id, rate limiting.

## Seguridad

- Cifrado AES-256-GCM para `encrypted_p12` y `encrypted_password`.
- Clave desde variable de entorno `CERT_ENCRYPTION_KEY` (base64 de 32 bytes).
- Nunca se loguea contraseña o binario del certificado.

### Generar clave

```bash
openssl rand -base64 32
```

## Endpoints

- `POST /certificates` (multipart: `file`, `password`, `name`)
- `GET /certificates`
- `GET /certificates/{id}`
- `POST /certificates/{id}/activate`
- `DELETE /certificates/{id}`
- `POST /certificates/sign-xml`

> Todos requieren header `X-Tenant-Id`.

## Método interno de firma

Implementado en `ICertificateSigningService.ObtenerCertificadoParaFirmaAsync(tenantId)`:

1. Busca certificado activo del tenant.
2. Descifra p12 y password.
3. Retorna `X509Certificate2` listo para firmar.

## Firmar XML

`IFirmaXmlService.FirmarXmlAsync(xml, tenantId)` aplica firma XML enveloped con `SignedXml`.

## Observabilidad

- Logs estructurados con Serilog.
- Correlation ID en middleware (`X-Correlation-Id`).
- Exportación a Loki para dashboards en Grafana.

## Docker

```bash
docker compose up --build
```

Servicios incluidos: API, PostgreSQL, Redis, Loki y Grafana.
