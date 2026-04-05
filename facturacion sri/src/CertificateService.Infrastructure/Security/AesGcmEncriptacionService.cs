using CertificateService.Application.Interfaces;
using System.Security.Cryptography;

namespace CertificateService.Infrastructure.Security;

public class AesGcmEncriptacionService : IEncriptacionService
{
    private readonly byte[] _key;

    public AesGcmEncriptacionService()
    {
        var keyBase64 = Environment.GetEnvironmentVariable("CERT_ENCRYPTION_KEY")
            ?? throw new InvalidOperationException("No se encontró CERT_ENCRYPTION_KEY en variables de entorno.");

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("CERT_ENCRYPTION_KEY debe tener 32 bytes codificados en base64 para AES-256.");
        }
    }

    public byte[] Encriptar(byte[] datos)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cifrado = new byte[datos.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, datos, cifrado, tag);

        return Combinar(nonce, tag, cifrado);
    }

    public byte[] Desencriptar(byte[] datosCifrados)
    {
        if (datosCifrados.Length < 28)
            throw new CryptographicException("Payload cifrado inválido.");

        var nonce = datosCifrados[..12];
        var tag = datosCifrados[12..28];
        var cifrado = datosCifrados[28..];
        var plano = new byte[cifrado.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cifrado, tag, plano);

        return plano;
    }

    private static byte[] Combinar(byte[] nonce, byte[] tag, byte[] cifrado)
    {
        var resultado = new byte[nonce.Length + tag.Length + cifrado.Length];
        Buffer.BlockCopy(nonce, 0, resultado, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, resultado, nonce.Length, tag.Length);
        Buffer.BlockCopy(cifrado, 0, resultado, nonce.Length + tag.Length, cifrado.Length);
        return resultado;
    }
}
