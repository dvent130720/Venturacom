namespace CertificateService.Application.Interfaces;

public interface IEncriptacionService
{
    byte[] Encriptar(byte[] datos);
    byte[] Desencriptar(byte[] datosCifrados);
}
