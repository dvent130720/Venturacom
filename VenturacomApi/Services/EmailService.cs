using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace VenturacomApi.Services;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string otp, CancellationToken ct = default);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOtpAsync(string toEmail, string otp, CancellationToken ct = default)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var fromEmail = _config["Email:FromEmail"] ?? "noreply@venturacomp.com";
        var fromName = _config["Email:FromName"] ?? "VenturaComp";
        var username = _config["Email:Username"] ?? "";
        var password = _config["Email:Password"] ?? "";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Tu código de acceso VenturaComp";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"/></head>
            <body style="font-family:sans-serif;background:#080812;color:#e2e8f0;padding:40px;">
              <div style="max-width:480px;margin:auto;background:#0f0f1a;border:1px solid rgba(108,99,255,0.3);border-radius:20px;padding:40px;text-align:center;">
                <h1 style="color:#a78bfa;margin-bottom:8px;">VenturaComp</h1>
                <p style="color:#64748b;margin-bottom:32px;">Tu código de verificación es:</p>
                <div style="font-size:40px;font-weight:800;letter-spacing:12px;color:#f1f5f9;background:rgba(108,99,255,0.12);border:1px solid rgba(108,99,255,0.3);border-radius:12px;padding:20px;margin-bottom:24px;">
                  {otp}
                </div>
                <p style="color:#475569;font-size:14px;">Este código expira en <strong>10 minutos</strong>.</p>
                <p style="color:#334155;font-size:12px;margin-top:24px;">Si no solicitaste este código, ignora este mensaje.</p>
              </div>
            </body>
            </html>
            """
        };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            // In development, log the OTP to console
            _logger.LogWarning("DEV – OTP for {Email}: {Otp}", toEmail, otp);
            throw;
        }
    }
}
