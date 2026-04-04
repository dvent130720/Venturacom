namespace SRI.Facturacion.Application.Contracts;
public interface ICertificateProtector { byte[] EncryptP12(byte[] p12Bytes, string encryptionKey, out byte[] iv); byte[] DecryptP12(byte[] encrypted, byte[] iv, string encryptionKey); }
