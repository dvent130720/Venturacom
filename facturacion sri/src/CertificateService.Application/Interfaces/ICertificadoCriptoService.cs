using System.Security.Cryptography.X509Certificates;

namespace CertificateService.Application.Interfaces;

public interface ICertificadoCriptoService
{
    X509Certificate2 CargarYValidar(byte[] archivoP12, string password);
}
