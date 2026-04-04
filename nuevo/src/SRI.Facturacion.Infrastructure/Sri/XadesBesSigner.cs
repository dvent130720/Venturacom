using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using SRI.Facturacion.Application.Contracts;
namespace SRI.Facturacion.Infrastructure.Sri;
public sealed class XadesBesSigner : IXadesSigner {
  public string Sign(string xml, byte[] p12, string runtimePassword){
    var cert=new X509Certificate2(p12,runtimePassword,X509KeyStorageFlags.Exportable|X509KeyStorageFlags.EphemeralKeySet);
    var doc=new XmlDocument{PreserveWhitespace=true}; doc.LoadXml(xml);
    var signedXml=new SignedXml(doc){SigningKey=cert.GetRSAPrivateKey()};
    var reference=new Reference(""); reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
    signedXml.AddReference(reference); signedXml.KeyInfo=new KeyInfo(); signedXml.KeyInfo.AddClause(new KeyInfoX509Data(cert)); signedXml.ComputeSignature();
    doc.DocumentElement!.AppendChild(doc.ImportNode(signedXml.GetXml(),true)); return doc.OuterXml;
  }
}
