namespace SRI.Facturacion.Application.Contracts;
public interface IXadesSigner { string Sign(string xml, byte[] p12, string runtimePassword); }
