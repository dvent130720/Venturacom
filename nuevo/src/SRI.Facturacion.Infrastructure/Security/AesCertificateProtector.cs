using System.Security.Cryptography;
using SRI.Facturacion.Application.Contracts;
namespace SRI.Facturacion.Infrastructure.Security;
public sealed class AesCertificateProtector : ICertificateProtector {
  public byte[] EncryptP12(byte[] p12Bytes, string encryptionKey, out byte[] iv){ using var aes=Aes.Create(); aes.Key=SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(encryptionKey)); aes.GenerateIV(); iv=aes.IV; using var enc=aes.CreateEncryptor(); return enc.TransformFinalBlock(p12Bytes,0,p12Bytes.Length); }
  public byte[] DecryptP12(byte[] encrypted, byte[] iv, string encryptionKey){ using var aes=Aes.Create(); aes.Key=SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(encryptionKey)); aes.IV=iv; using var dec=aes.CreateDecryptor(); return dec.TransformFinalBlock(encrypted,0,encrypted.Length); }
}
