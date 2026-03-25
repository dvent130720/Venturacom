using Microsoft.EntityFrameworkCore;
using VenturacomSri.Api.Data;
using VenturacomSri.Api.DTOs;
using VenturacomSri.Api.Models;
using VenturacomSri.Api.Services.Sri;
using VenturacomSri.Api.Utils;

namespace VenturacomSri.Api.Services;

/// <summary>
/// Servicio de facturación.
///
/// Regla de seguridad: NO se puede enviar al SRI sin firma digital.
/// El método <see cref="SendToSriAsync"/> lanza si la factura no está firmada.
/// </summary>
public class InvoiceService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly XmlGeneratorService _xmlGen;
    private readonly XmlSignerService _xmlSigner;
    private readonly CertificateService _certService;
    private readonly SriClientService _sriClient;
    private readonly ILogger<InvoiceService> _logger;

    // IVA por código de porcentaje
    private static readonly Dictionary<string, decimal> IvaTariff = new()
    {
        ["0"]  = 0m,
        ["2"]  = 12m,
        ["3"]  = 14m,
        ["6"]  = 0m,
        ["7"]  = 0m,
        ["10"] = 15m,
    };

    public InvoiceService(
        AppDbContext db,
        IConfiguration cfg,
        XmlGeneratorService xmlGen,
        XmlSignerService xmlSigner,
        CertificateService certService,
        SriClientService sriClient,
        ILogger<InvoiceService> logger)
    {
        _db          = db;
        _cfg         = cfg;
        _xmlGen      = xmlGen;
        _xmlSigner   = xmlSigner;
        _certService = certService;
        _sriClient   = sriClient;
        _logger      = logger;
    }

    // ── Crear factura (estado DRAFT) ──────────────────────────────
    public async Task<Invoice> CreateAsync(CreateInvoiceRequest req)
    {
        var issuerRuc = _cfg["Issuer:Ruc"]!;
        var env       = int.Parse(_cfg["Sri:Environment"] ?? "1");
        var estab     = _cfg["Issuer:Establishment"] ?? "001";
        var ptoEmi    = _cfg["Issuer:EmissionPoint"] ?? "001";
        var issueDate = req.IssueDate ?? DateTime.UtcNow;

        var sequential = await GetNextSequentialAsync(estab, ptoEmi);
        var accessKey  = AccessKeyGenerator.Generate(
            issueDate, issuerRuc, env, estab, ptoEmi, sequential);

        // Calcular totales
        decimal subtotal = 0, totalDiscount = 0, totalIva = 0;
        var itemsData = new List<InvoiceItem>();

        foreach (var itemReq in req.Items)
        {
            var qty      = itemReq.Quantity;
            var price    = itemReq.UnitPrice;
            var disc     = itemReq.Discount;
            var lineBase = qty * price - disc;
            var rate     = IvaTariff.GetValueOrDefault(itemReq.TaxPercentageCode, 0m);
            var taxVal   = Math.Round(lineBase * rate / 100, 2);

            subtotal      += lineBase;
            totalDiscount += disc;
            totalIva      += taxVal;

            itemsData.Add(new InvoiceItem
            {
                MainCode         = itemReq.MainCode,
                AuxCode          = itemReq.AuxCode,
                Description      = itemReq.Description,
                Quantity         = qty,
                UnitPrice        = price,
                Discount         = disc,
                SubtotalNoTax    = lineBase,
                TaxCode          = "2",
                TaxPercentageCode = itemReq.TaxPercentageCode,
                TaxRate          = rate,
                TaxBase          = lineBase,
                TaxValue         = taxVal,
            });
        }

        var total = subtotal + totalIva;

        // Buscar certificado activo si no se especificó
        var certId = req.CertificateId
            ?? (await _certService.GetActiveByRucAsync(issuerRuc))?.Id;

        var invoice = new Invoice
        {
            AccessKey       = accessKey,
            Status          = InvoiceStatus.Draft,
            Environment     = env,
            IssuerRuc       = issuerRuc,
            IssuerName      = _cfg["Issuer:BusinessName"]!,
            IssuerTradeName = _cfg["Issuer:TradeName"],
            IssuerAddress   = _cfg["Issuer:Address"]!,
            Establishment   = estab,
            EmissionPoint   = ptoEmi,
            Sequential      = sequential,
            IssueDate       = issueDate,
            BuyerIdType     = req.BuyerIdType,
            BuyerId         = req.BuyerId,
            BuyerName       = req.BuyerName,
            BuyerEmail      = req.BuyerEmail,
            BuyerAddress    = req.BuyerAddress,
            Subtotal        = Math.Round(subtotal, 2),
            TotalDiscount   = Math.Round(totalDiscount, 2),
            TotalIva        = Math.Round(totalIva, 2),
            TotalAmount     = Math.Round(total, 2),
            PaymentMethod   = req.PaymentMethod,
            PaymentAmount   = Math.Round(total, 2),
            CertificateId   = certId,
        };

        invoice.Items = itemsData;
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        await AddAuditAsync(invoice.Id, "CREATED",
            $"Factura {AccessKeyGenerator.FormatNumber(estab, ptoEmi, sequential)} creada");

        _logger.LogInformation("Factura creada {Id} clave:{Key}", invoice.Id, accessKey);
        return invoice;
    }

    // ── Firmar factura ────────────────────────────────────────────
    public async Task<Invoice> SignAsync(string invoiceId)
    {
        var invoice = await LoadWithItemsAsync(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException(
                $"Solo se pueden firmar facturas DRAFT. Estado actual: {invoice.Status}.");

        if (string.IsNullOrEmpty(invoice.CertificateId))
            throw new InvalidOperationException(
                "La factura no tiene certificado asignado. " +
                "Suba un certificado P12 y asígnelo antes de firmar.");

        // Generar XML sin firma
        var xml = _xmlGen.GenerateInvoiceXml(invoice);

        // Obtener P12 descifrado y firmar
        var (p12, pwd) = await _certService.GetDecryptedAsync(invoice.CertificateId);
        var cert509    = _xmlSigner.LoadCertificate(p12, pwd);
        var signedXml  = _xmlSigner.SignXml(xml, cert509);

        invoice.Status     = InvoiceStatus.Signed;
        invoice.XmlContent = xml;
        invoice.SignedXml  = signedXml;
        invoice.UpdatedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AddAuditAsync(invoiceId, "SIGNED", "XML firmado con XAdES-BES");
        _logger.LogInformation("Factura firmada {Id}", invoiceId);
        return invoice;
    }

    // ── Enviar al SRI (solo facturas FIRMADAS) ────────────────────
    public async Task<Invoice> SendToSriAsync(string invoiceId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FindAsync(new object[] { invoiceId }, ct)
            ?? throw new KeyNotFoundException(invoiceId);

        if (invoice.Status != InvoiceStatus.Signed)
            throw new InvalidOperationException(
                $"No se puede enviar al SRI una factura sin firma electrónica. " +
                $"Estado: {invoice.Status}. Firme primero la factura con POST /invoices/{invoiceId}/sign.");

        if (string.IsNullOrEmpty(invoice.SignedXml))
            throw new InvalidOperationException(
                "El XML firmado no está disponible. Firme la factura primero.");

        var result = await _sriClient.SendToReceptionAsync(invoice.SignedXml, ct);

        if (result.State == ReceptionState.Received)
        {
            invoice.Status      = InvoiceStatus.Sent;
            invoice.SriResponse = System.Text.Json.JsonSerializer.Serialize(result);
            invoice.UpdatedAt   = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AddAuditAsync(invoiceId, "SENT",
                $"Recibida por SRI. Mensajes: {string.Join("; ", result.Messages.Select(m => m.Message))}");
        }
        else
        {
            var reason = string.Join("; ", result.Messages.Select(m => m.Message));
            invoice.Status      = InvoiceStatus.Rejected;
            invoice.RejectReason = reason;
            invoice.SriResponse  = System.Text.Json.JsonSerializer.Serialize(result);
            invoice.UpdatedAt    = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AddAuditAsync(invoiceId, "REJECTED", $"SRI devolvió: {reason}");
            _logger.LogWarning("SRI rechazó la factura {Id}: {Reason}", invoiceId, reason);
        }

        return invoice;
    }

    // ── Verificar autorización ────────────────────────────────────
    public async Task<Invoice> CheckAuthorizationAsync(string invoiceId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FindAsync(new object[] { invoiceId }, ct)
            ?? throw new KeyNotFoundException(invoiceId);

        if (invoice.Status != InvoiceStatus.Sent)
            throw new InvalidOperationException(
                $"Solo se puede verificar la autorización de facturas SENT. Estado: {invoice.Status}.");

        var auth = await _sriClient.CheckAuthorizationAsync(invoice.AccessKey, ct);

        if (auth.State == AuthorizationState.Authorized)
        {
            invoice.Status     = InvoiceStatus.Authorized;
            invoice.AuthNumber = auth.AuthNumber;
            invoice.AuthDate   = DateTime.TryParse(auth.AuthDate, out var d) ? d.ToUniversalTime() : null;
            invoice.AuthXml    = auth.Comprobante;
            invoice.UpdatedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AddAuditAsync(invoiceId, "AUTHORIZED",
                $"Autorizada SRI. Número: {auth.AuthNumber}");
            _logger.LogInformation("Factura autorizada {Id} auth:{Num}", invoiceId, auth.AuthNumber);
        }
        else
        {
            var reason = string.Join("; ", auth.Messages.Select(m => m.Message));
            invoice.Status       = InvoiceStatus.Rejected;
            invoice.RejectReason = reason;
            invoice.UpdatedAt    = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AddAuditAsync(invoiceId, "REJECTED", $"SRI no autorizó: {reason}");
            _logger.LogWarning("Factura no autorizada {Id}: {Reason}", invoiceId, reason);
        }

        return invoice;
    }

    // ── Cancelar (solo DRAFT o SIGNED) ────────────────────────────
    public async Task<Invoice> CancelAsync(string invoiceId, string? reason = null)
    {
        var invoice = await _db.Invoices.FindAsync(invoiceId)
            ?? throw new KeyNotFoundException(invoiceId);

        if (invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.Signed))
            throw new InvalidOperationException(
                $"Solo se pueden cancelar facturas DRAFT o SIGNED. Estado: {invoice.Status}.");

        invoice.Status    = InvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddAuditAsync(invoiceId, "CANCELLED", reason ?? "Cancelada manualmente");
        return invoice;
    }

    // ── Consultas ─────────────────────────────────────────────────
    public async Task<Invoice> GetAsync(string id)
        => await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.AuditLogs.OrderBy(a => a.CreatedAt))
            .FirstOrDefaultAsync(i => i.Id == id)
        ?? throw new KeyNotFoundException(id);

    public async Task<(int Total, List<Invoice> Items)> ListAsync(
        InvoiceStatus? status, int page, int limit)
    {
        var query = _db.Invoices
            .Include(i => i.Items)
            .AsQueryable();
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (total, items);
    }

    // ── Helpers ───────────────────────────────────────────────────
    private async Task<Invoice> LoadWithItemsAsync(string id)
        => await _db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id)
        ?? throw new KeyNotFoundException(id);

    private async Task<string> GetNextSequentialAsync(string estab, string ptoEmi)
    {
        var last = await _db.Invoices
            .Where(i => i.Establishment == estab && i.EmissionPoint == ptoEmi)
            .OrderByDescending(i => i.Sequential)
            .Select(i => i.Sequential)
            .FirstOrDefaultAsync();

        var next = last is null ? 1 : int.Parse(last) + 1;
        return next.ToString().PadLeft(9, '0');
    }

    private async Task AddAuditAsync(string invoiceId, string action, string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            InvoiceId = invoiceId,
            Action    = action,
            Details   = details,
        });
        await _db.SaveChangesAsync();
    }
}
