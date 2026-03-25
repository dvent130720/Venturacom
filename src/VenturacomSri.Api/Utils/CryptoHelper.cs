using System.Security.Cryptography;
using System.Text;

namespace VenturacomSri.Api.Utils;

/// <summary>
/// Cifrado simétrico AES-256-GCM para proteger certificados P12
/// y contraseñas almacenados en la base de datos.
///
/// Formato: base64(nonce):base64(tag):base64(ciphertext)
/// </summary>
public static class CryptoHelper
{
    private const int NonceSizeBytes = 12; // 96 bits — estándar para GCM
    private const int TagSizeBytes = 16;   // 128 bits

    private static byte[] GetKey(string hexKey)
    {
        if (hexKey.Length != 64)
            throw new InvalidOperationException("ENCRYPTION_KEY debe ser 32 bytes (64 chars hex).");
        return Convert.FromHexString(hexKey);
    }

    public static string Encrypt(string plaintext, string hexKey)
    {
        var key = GetKey(hexKey);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
    }

    public static string Decrypt(string encryptedText, string hexKey)
    {
        var parts = encryptedText.Split(':');
        if (parts.Length != 3)
            throw new ArgumentException("Formato de texto cifrado inválido.");

        var key = GetKey(hexKey);
        var nonce = Convert.FromBase64String(parts[0]);
        var tag = Convert.FromBase64String(parts[1]);
        var ciphertext = Convert.FromBase64String(parts[2]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
