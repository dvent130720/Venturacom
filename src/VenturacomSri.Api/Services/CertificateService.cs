using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using VenturacomSri.Api.Data;
using VenturacomSri.Api.Models;
using VenturacomSri.Api.Services.Sri;
using VenturacomSri.Api.Utils;

namespace VenturacomSri.Api.Services;

public class CertificateService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly XmlSignerService _signer;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        AppDbContext db,
        IConfiguration cfg,
        XmlSignerService signer,
        ILogger<CertificateService> logger)
    {
        _db     = db;
        _cfg    = cfg;
        _signer = signer;
        _logger = logger;
    }

    private string EncKey => _cfg["Encryption:Key"]
        ?? throw new InvalidOperationException("Encryption:Key no configurado.");

    // ── Subir certificado P12 ─────────────────────────────────────
    public async Task<Certificate> UploadAsync(
        string name, byte[] p12Buffer, string password)
    {
        // Validar que el P12 sea legible y vigente (lanza si no)
        var cert509 = _signer.LoadCertificate(p12Buffer, password);

        // Extraer RUC del Subject (CN o serialNumber)
        var ruc = ExtractRucFromSubject(cert509);

        var encP12  = CryptoHelper.Encrypt(Convert.ToBase64String(p12Buffer), EncKey);
        var encPass = CryptoHelper.Encrypt(password, EncKey);

        var cert = new Certificate
        {
            Name             = name,
            Ruc              = ruc,
            EncryptedP12     = encP12,
            EncryptedPassword = encPass,
            ValidFrom        = cert509.NotBefore.ToUniversalTime(),
            ValidUntil       = cert509.NotAfter.ToUniversalTime(),
            Active           = true,
        };

        _db.Certificates.Add(cert);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Certificado subido {Id} RUC:{Ruc} válido hasta {Until}",
            cert.Id, ruc, cert.ValidUntil.ToString("yyyy-MM-dd"));

        return cert;
    }

    // ── Obtener P12 descifrado ────────────────────────────────────
    public async Task<(byte[] P12Buffer, string Password)> GetDecryptedAsync(string certificateId)
    {
        var cert = await _db.Certificates.FindAsync(certificateId)
            ?? throw new KeyNotFoundException($"Certificado {certificateId} no encontrado.");

        if (!cert.Active)
            throw new InvalidOperationException("El certificado está desactivado.");

        if (cert.ValidUntil < DateTime.UtcNow)
            throw new InvalidOperationException(
                $"El certificado expiró el {cert.ValidUntil:yyyy-MM-dd}.");

        var p12Base64 = CryptoHelper.Decrypt(cert.EncryptedP12, EncKey);
        var password  = CryptoHelper.Decrypt(cert.EncryptedPassword, EncKey);
        var p12Buffer = Convert.FromBase64String(p12Base64);

        return (p12Buffer, password);
    }

    // ── Certificado activo por RUC ────────────────────────────────
    public async Task<Certificate?> GetActiveByRucAsync(string ruc)
        => await _db.Certificates
            .Where(c => c.Ruc == ruc && c.Active && c.ValidUntil > DateTime.UtcNow)
            .OrderByDescending(c => c.ValidUntil)
            .FirstOrDefaultAsync();

    // ── Listar ────────────────────────────────────────────────────
    public async Task<List<CertificateSummary>> ListAsync()
        => await _db.Certificates
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CertificateSummary(
                c.Id, c.Name, c.Ruc,
                c.ValidFrom, c.ValidUntil, c.Active))
            .ToListAsync();

    // ── Activar / desactivar ──────────────────────────────────────
    public async Task<Certificate> SetActiveAsync(string id, bool active)
    {
        var cert = await _db.Certificates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Certificado {id} no encontrado.");
        cert.Active    = active;
        cert.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return cert;
    }

    // ── Helpers ───────────────────────────────────────────────────
    private static string ExtractRucFromSubject(X509Certificate2 cert)
    {
        // Intenta extraer el RUC del campo CN o serialNumber del Subject
        var parts = cert.Subject.Split(',');
        foreach (var p in parts)
        {
            var kv = p.Trim().Split('=');
            if (kv.Length == 2 &&
                (kv[0].Trim().Equals("CN", StringComparison.OrdinalIgnoreCase) ||
                 kv[0].Trim().Equals("SERIALNUMBER", StringComparison.OrdinalIgnoreCase)))
            {
                var digits = new string(kv[1].Where(char.IsDigit).ToArray());
                if (digits.Length >= 10)
                    return digits[..13].PadRight(13, '0')[..13];
            }
        }
        return "0000000000000";
    }
}

public record CertificateSummary(
    string Id, string Name, string Ruc,
    DateTime ValidFrom, DateTime ValidUntil, bool Active);
