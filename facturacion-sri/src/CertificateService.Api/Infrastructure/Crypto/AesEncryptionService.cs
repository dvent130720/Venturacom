using System.Security.Cryptography;
using CertificateService.Api.Application.Abstractions;

namespace CertificateService.Api.Infrastructure.Crypto;

public sealed class AesEncryptionService(IConfiguration configuration) : IEncryptionService
{
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int KeySize = 32;
    private readonly byte[] _masterKey = Convert.FromBase64String(configuration["Encryption:MasterKey"]
        ?? throw new InvalidOperationException("Encryption:MasterKey is required and must be base64."));

    public byte[] Encrypt(byte[] plainBytes)
    {
        using var aes = Aes.Create();
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iv = RandomNumberGenerator.GetBytes(IvSize);
        using var derive = new Rfc2898DeriveBytes(_masterKey, salt, 100_000, HashAlgorithmName.SHA256);
        aes.Key = derive.GetBytes(KeySize);
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Combine(salt, iv, cipherBytes);
    }

    public byte[] Decrypt(byte[] cipherBytes)
    {
        var salt = cipherBytes[..SaltSize];
        var iv = cipherBytes[SaltSize..(SaltSize + IvSize)];
        var payload = cipherBytes[(SaltSize + IvSize)..];

        using var aes = Aes.Create();
        using var derive = new Rfc2898DeriveBytes(_masterKey, salt, 100_000, HashAlgorithmName.SHA256);
        aes.Key = derive.GetBytes(KeySize);
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(payload, 0, payload.Length);
    }

    public string EncryptString(string plainText) => Convert.ToBase64String(Encrypt(System.Text.Encoding.UTF8.GetBytes(plainText)));

    public string DecryptString(string cipherText) => System.Text.Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(cipherText)));

    private static byte[] Combine(params byte[][] arrays)
    {
        var result = new byte[arrays.Sum(x => x.Length)];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }
        return result;
    }
}
