namespace CertificateService.Api.Application.Abstractions;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] plainBytes);
    byte[] Decrypt(byte[] cipherBytes);
    string EncryptString(string plainText);
    string DecryptString(string cipherText);
}
