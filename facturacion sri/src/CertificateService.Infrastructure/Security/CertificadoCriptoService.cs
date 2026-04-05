using CertificateService.Application.Interfaces;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CertificateService.Infrastructure.Security;

public class CertificadoCriptoService : ICertificadoCriptoService
{
    public X509Certificate2 CargarYValidar(byte[] archivoP12, string password)
    {
        if (archivoP12.Length == 0)
            throw new InvalidOperationException("El archivo del certificado está vacío.");

        try
        {
            var cert = new X509Certificate2(archivoP12, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

            if (!cert.HasPrivateKey)
                throw new InvalidOperationException("El certificado no contiene clave privada.");

            return cert;
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Certificado o password inválidos.");
        }
    }
}
