using CertificateService.Application.Interfaces;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace CertificateService.Infrastructure.Services;

public class FirmaXmlService(ICertificateSigningService certificateSigningService) : IFirmaXmlService
{
    public async Task<string> FirmarXmlAsync(string xml, Guid tenantId, CancellationToken cancellationToken)
    {
        var (certificado, _) = await certificateSigningService.ObtenerCertificadoParaFirmaAsync(tenantId, cancellationToken);

        var documento = new XmlDocument { PreserveWhitespace = true };
        documento.LoadXml(xml);

        var firmado = new SignedXml(documento)
        {
            SigningKey = certificado.GetRSAPrivateKey()
        };

        var referencia = new Reference(string.Empty);
        referencia.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        referencia.AddTransform(new XmlDsigC14NTransform());
        firmado.AddReference(referencia);

        var infoClave = new KeyInfo();
        infoClave.AddClause(new KeyInfoX509Data(certificado));
        firmado.KeyInfo = infoClave;

        firmado.ComputeSignature();
        var xmlFirma = firmado.GetXml();

        documento.DocumentElement?.AppendChild(documento.ImportNode(xmlFirma, true));
        return documento.OuterXml;
    }
}
