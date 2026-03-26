using System.Xml;

namespace VenturacomSri.Api.Services.Sri;

/// <summary>
/// Cliente SOAP para comunicación con el SRI Ecuador.
///
/// Ambiente de PRUEBAS (SriEnvironment=1):
///   Recepción:    https://celcer.sri.gob.ec/…/RecepcionComprobantesOffline
///   Autorización: https://celcer.sri.gob.ec/…/AutorizacionComprobantesOffline
/// </summary>
public class SriClientService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    private readonly ILogger<SriClientService> _logger;

    public SriClientService(HttpClient http, IConfiguration cfg, ILogger<SriClientService> logger)
    {
        _http   = http;
        _cfg    = cfg;
        _logger = logger;
    }

    // ── Recepción ─────────────────────────────────────────────────
    public async Task<ReceptionResult> SendToReceptionAsync(
        string signedXml, CancellationToken ct = default)
    {
        var base64Xml = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(signedXml));
        var envelope = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope
              xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
              xmlns:ec="http://ec.gob.sri.ws.recepcion">
              <soapenv:Header/>
              <soapenv:Body>
                <ec:validarComprobante>
                  <xml>{base64Xml}</xml>
                </ec:validarComprobante>
              </soapenv:Body>
            </soapenv:Envelope>
            """;

        var url = _cfg["Sri:ReceptionUrl"]!;
        _logger.LogDebug("Enviando comprobante a recepción SRI: {Url}", url);

        var response = await PostSoapAsync(url, envelope, ct);
        return ParseReceptionResponse(response);
    }

    // ── Autorización ──────────────────────────────────────────────
    public async Task<AuthorizationResult> CheckAuthorizationAsync(
        string accessKey, CancellationToken ct = default)
    {
        var envelope = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope
              xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
              xmlns:ec="http://ec.gob.sri.ws.autorizacion">
              <soapenv:Header/>
              <soapenv:Body>
                <ec:autorizacionComprobante>
                  <claveAccesoComprobante>{accessKey}</claveAccesoComprobante>
                </ec:autorizacionComprobante>
              </soapenv:Body>
            </soapenv:Envelope>
            """;

        var url = _cfg["Sri:AuthorizationUrl"]!;
        _logger.LogDebug("Consultando autorización SRI: {AccessKey}", accessKey);

        var response = await PostSoapAsync(url, envelope, ct);
        return ParseAuthorizationResponse(response);
    }

    // ── HTTP ──────────────────────────────────────────────────────
    private async Task<string> PostSoapAsync(string url, string envelope, CancellationToken ct)
    {
        var content = new StringContent(envelope, System.Text.Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "\"\"");

        var resp = await _http.PostAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    // ── Parseo de respuestas ──────────────────────────────────────
    private static ReceptionResult ParseReceptionResponse(string soapXml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(soapXml);

        var state   = GetText(doc, "estado");
        var messages = GetMessages(doc);

        return new ReceptionResult(
            state == "RECIBIDA" ? ReceptionState.Received : ReceptionState.Returned,
            messages);
    }

    private static AuthorizationResult ParseAuthorizationResponse(string soapXml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(soapXml);

        var authNum  = GetText(doc, "numeroAutorizacion");
        var authDate = GetText(doc, "fechaAutorizacion");
        var state    = GetText(doc, "estado");
        var comp     = GetText(doc, "comprobante");
        var msgs     = GetMessages(doc);

        return new AuthorizationResult(
            authNum, authDate,
            state == "AUTORIZADO" ? AuthorizationState.Authorized : AuthorizationState.NotAuthorized,
            comp, msgs);
    }

    private static string GetText(XmlDocument doc, string tagName)
        => doc.GetElementsByTagName(tagName)[0]?.InnerText?.Trim() ?? "";

    private static List<SriMessage> GetMessages(XmlDocument doc)
    {
        var list = new List<SriMessage>();
        var nodes = doc.GetElementsByTagName("mensaje");
        foreach (XmlNode n in nodes)
        {
            list.Add(new SriMessage(
                GetChildText(n, "identificador"),
                GetChildText(n, "mensaje"),
                GetChildText(n, "informacionAdicional"),
                GetChildText(n, "tipo")));
        }
        return list;
    }

    private static string GetChildText(XmlNode parent, string tag)
        => parent[tag]?.InnerText?.Trim() ?? "";
}

// ── DTOs de resultado ─────────────────────────────────────────────
public enum ReceptionState   { Received, Returned }
public enum AuthorizationState { Authorized, NotAuthorized }

public record SriMessage(
    string Identifier,
    string Message,
    string? AdditionalInfo,
    string Type);

public record ReceptionResult(
    ReceptionState State,
    List<SriMessage> Messages);

public record AuthorizationResult(
    string AuthNumber,
    string AuthDate,
    AuthorizationState State,
    string Comprobante,
    List<SriMessage> Messages);
