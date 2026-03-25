using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace VenturacomSri.Api.Services.Sri;

/// <summary>
/// Firma XML con XAdES-BES para comprobantes electrónicos SRI Ecuador.
///
/// Proceso:
///   1. Cargar certificado X.509 desde buffer P12
///   2. Calcular digest C14N del comprobante (referencia #comprobante)
///   3. Construir XAdES SignedProperties y calcular su digest
///   4. Construir SignedInfo con ambas referencias
///   5. Firmar SignedInfo con RSA-SHA1
///   6. Ensamblar &lt;Signature&gt; y annexarlo al XML
///
/// La firma NO se puede realizar sin un certificado P12 válido y vigente.
/// </summary>
public class XmlSignerService
{
    private const string DsNs    = "http://www.w3.org/2000/09/xmldsig#";
    private const string XadesNs = "http://uri.etsi.org/01903/v1.3.2#";
    private const string C14NAlg = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
    private const string RsaSha1 = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
    private const string Sha1Alg = "http://www.w3.org/2000/09/xmldsig#sha1";
    private const string EnvSigT = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
    private const string XadesType = "http://uri.etsi.org/01903#SignedProperties";

    // ── Carga del certificado ─────────────────────────────────────
    public X509Certificate2 LoadCertificate(byte[] p12Buffer, string password)
    {
        var cert = new X509Certificate2(
            p12Buffer,
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

        if (cert.NotAfter < DateTime.UtcNow)
            throw new InvalidOperationException(
                $"El certificado expiró el {cert.NotAfter:yyyy-MM-dd}. No puede usarse para firmar.");

        if (cert.GetRSAPrivateKey() is null)
            throw new InvalidOperationException("El certificado no contiene clave privada RSA.");

        return cert;
    }

    // ── Firma principal ───────────────────────────────────────────
    public string SignXml(string xmlContent, X509Certificate2 certificate)
    {
        const string sigId   = "Signature1";
        const string xadesId = "xades-Signature1";

        var xmlDoc = new XmlDocument { PreserveWhitespace = false };
        xmlDoc.LoadXml(xmlContent);

        using var rsa = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Clave privada RSA no disponible.");

        // 1. Digest del comprobante (C14N del documento; sin firma aún)
        byte[] docDigest = DigestDocument(xmlDoc);

        // 2. XAdES SignedProperties
        var xadesDoc = BuildXadesSignedProperties(certificate, xadesId, sigId);
        var signedPropsEl = (XmlElement)xadesDoc.GetElementsByTagName("SignedProperties", XadesNs)[0]!;
        byte[] xadesDigest = DigestElement(signedPropsEl);

        // 3. SignedInfo
        var siDoc = BuildSignedInfo(sigId, xadesId, docDigest, xadesDigest);
        byte[] siBytes = CanonicalizeDocument(siDoc);

        // 4. Firma RSA-SHA1 sobre el C14N de SignedInfo
        byte[] sigBytes = rsa.SignData(siBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

        // 5. Ensamblar <Signature> y annexar al XML
        var signatureEl = BuildSignatureElement(
            xmlDoc, sigId, xadesId,
            siDoc, sigBytes,
            certificate, xadesDoc);

        xmlDoc.DocumentElement!.AppendChild(signatureEl);
        return xmlDoc.OuterXml;
    }

    // ── Construcción de XAdES SignedProperties ────────────────────
    private static XmlDocument BuildXadesSignedProperties(
        X509Certificate2 cert, string xadesId, string sigId)
    {
        var doc = new XmlDocument();

        var qp = doc.CreateElement("xades", "QualifyingProperties", XadesNs);
        qp.SetAttribute("xmlns:xades", XadesNs);
        qp.SetAttribute("xmlns:ds", DsNs);
        qp.SetAttribute("Target", $"#{sigId}");
        doc.AppendChild(qp);

        var sp = doc.CreateElement("xades", "SignedProperties", XadesNs);
        sp.SetAttribute("Id", xadesId);
        qp.AppendChild(sp);

        var ssp = doc.CreateElement("xades", "SignedSignatureProperties", XadesNs);
        sp.AppendChild(ssp);

        // SigningTime
        var st = doc.CreateElement("xades", "SigningTime", XadesNs);
        st.InnerText = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        ssp.AppendChild(st);

        // SigningCertificate
        var scEl = doc.CreateElement("xades", "SigningCertificate", XadesNs);
        ssp.AppendChild(scEl);
        var certEl = doc.CreateElement("xades", "Cert", XadesNs);
        scEl.AppendChild(certEl);

        var cdEl = doc.CreateElement("xades", "CertDigest", XadesNs);
        certEl.AppendChild(cdEl);
        var dmEl = doc.CreateElement("ds", "DigestMethod", DsNs);
        dmEl.SetAttribute("Algorithm", Sha1Alg);
        cdEl.AppendChild(dmEl);
        var dvEl = doc.CreateElement("ds", "DigestValue", DsNs);
        dvEl.InnerText = Convert.ToBase64String(SHA1.HashData(cert.RawData));
        cdEl.AppendChild(dvEl);

        var isEl = doc.CreateElement("xades", "IssuerSerial", XadesNs);
        certEl.AppendChild(isEl);
        var inEl = doc.CreateElement("ds", "X509IssuerName", DsNs);
        inEl.InnerText = cert.Issuer;
        isEl.AppendChild(inEl);
        var snEl = doc.CreateElement("ds", "X509SerialNumber", DsNs);
        snEl.InnerText = cert.SerialNumber;
        isEl.AppendChild(snEl);

        return doc;
    }

    // ── Construcción de SignedInfo ────────────────────────────────
    private static XmlDocument BuildSignedInfo(
        string sigId, string xadesId,
        byte[] docDigest, byte[] xadesDigest)
    {
        var doc = new XmlDocument();

        var si = doc.CreateElement("ds", "SignedInfo", DsNs);
        si.SetAttribute("xmlns:ds", DsNs);
        doc.AppendChild(si);

        var cm = doc.CreateElement("ds", "CanonicalizationMethod", DsNs);
        cm.SetAttribute("Algorithm", C14NAlg);
        si.AppendChild(cm);

        var sm = doc.CreateElement("ds", "SignatureMethod", DsNs);
        sm.SetAttribute("Algorithm", RsaSha1);
        si.AppendChild(sm);

        // Referencia 1: #comprobante
        si.AppendChild(BuildRef(doc, "#comprobante", null, docDigest,
            new[] { EnvSigT, C14NAlg }));

        // Referencia 2: XAdES SignedProperties
        si.AppendChild(BuildRef(doc, $"#{xadesId}", XadesType, xadesDigest, []));

        return doc;
    }

    private static XmlElement BuildRef(
        XmlDocument doc, string uri, string? type,
        byte[] digest, string[] transforms)
    {
        var refEl = doc.CreateElement("ds", "Reference", DsNs);
        refEl.SetAttribute("URI", uri);
        if (type != null) refEl.SetAttribute("Type", type);

        if (transforms.Length > 0)
        {
            var ts = doc.CreateElement("ds", "Transforms", DsNs);
            refEl.AppendChild(ts);
            foreach (var t in transforms)
            {
                var tr = doc.CreateElement("ds", "Transform", DsNs);
                tr.SetAttribute("Algorithm", t);
                ts.AppendChild(tr);
            }
        }

        var dm = doc.CreateElement("ds", "DigestMethod", DsNs);
        dm.SetAttribute("Algorithm", Sha1Alg);
        refEl.AppendChild(dm);

        var dv = doc.CreateElement("ds", "DigestValue", DsNs);
        dv.InnerText = Convert.ToBase64String(digest);
        refEl.AppendChild(dv);

        return refEl;
    }

    // ── Ensamblado de <Signature> ─────────────────────────────────
    private static XmlElement BuildSignatureElement(
        XmlDocument targetDoc,
        string sigId, string xadesId,
        XmlDocument siDoc, byte[] sigBytes,
        X509Certificate2 cert,
        XmlDocument xadesDoc)
    {
        var sigEl = targetDoc.CreateElement("ds", "Signature", DsNs);
        sigEl.SetAttribute("xmlns:ds", DsNs);
        sigEl.SetAttribute("Id", sigId);

        // SignedInfo (importar desde siDoc)
        sigEl.AppendChild(targetDoc.ImportNode(siDoc.DocumentElement!, true));

        // SignatureValue
        var sv = targetDoc.CreateElement("ds", "SignatureValue", DsNs);
        sv.InnerText = Convert.ToBase64String(sigBytes);
        sigEl.AppendChild(sv);

        // KeyInfo
        var ki = targetDoc.CreateElement("ds", "KeyInfo", DsNs);
        sigEl.AppendChild(ki);
        var x509d = targetDoc.CreateElement("ds", "X509Data", DsNs);
        ki.AppendChild(x509d);
        var x509c = targetDoc.CreateElement("ds", "X509Certificate", DsNs);
        x509c.InnerText = Convert.ToBase64String(cert.RawData);
        x509d.AppendChild(x509c);

        // Object con XAdES QualifyingProperties
        var obj = targetDoc.CreateElement("ds", "Object", DsNs);
        obj.SetAttribute("Id", $"object-{sigId}");
        sigEl.AppendChild(obj);
        obj.AppendChild(targetDoc.ImportNode(xadesDoc.DocumentElement!, true));

        return sigEl;
    }

    // ── C14N helpers ─────────────────────────────────────────────
    private static byte[] DigestDocument(XmlDocument doc)
        => SHA1.HashData(CanonicalizeDocument(doc));

    private static byte[] DigestElement(XmlElement element)
        => SHA1.HashData(CanonicalizeElement(element));

    private static byte[] CanonicalizeDocument(XmlDocument doc)
    {
        var t = new XmlDsigC14NTransform(false);
        t.LoadInput(doc);
        using var ms = new MemoryStream();
        ((Stream)t.GetOutput(typeof(Stream))).CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] CanonicalizeElement(XmlElement element)
    {
        // Crear un documento temporal solo con el elemento para preservar
        // las declaraciones de namespace en el contexto correcto
        var tempDoc = new XmlDocument();
        tempDoc.AppendChild(tempDoc.ImportNode(element, true));
        return CanonicalizeDocument(tempDoc);
    }
}
