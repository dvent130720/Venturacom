using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace VenturacomSri.Api.Services.Auth;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOtpAsync(string toEmail, string code)
    {
        var smtpSection = _config.GetSection("Smtp");
        var host     = smtpSection["Host"]!;
        var port     = int.Parse(smtpSection["Port"] ?? "587");
        var user     = smtpSection["User"]!;
        var password = smtpSection["Password"]!;
        var fromName = smtpSection["FromName"] ?? "Venturacom";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, user));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Tu código de acceso - Venturacom";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <!DOCTYPE html>
            <html lang="es">
            <head><meta charset="UTF-8"></head>
            <body style="font-family:Arial,sans-serif;background:#f5f5f5;margin:0;padding:20px;">
              <div style="max-width:480px;margin:0 auto;background:#fff;border-radius:12px;padding:40px;box-shadow:0 2px 8px rgba(0,0,0,.08);">
                <h2 style="color:#1a1a2e;margin-bottom:8px;">Código de acceso</h2>
                <p style="color:#555;margin-bottom:32px;">Usa este código para ingresar a tu cuenta Venturacom. Expira en 10 minutos.</p>
                <div style="background:#f0f4ff;border-radius:8px;padding:24px;text-align:center;margin-bottom:32px;">
                  <span style="font-size:40px;font-weight:700;letter-spacing:12px;color:#4f46e5;">{code}</span>
                </div>
                <p style="color:#999;font-size:13px;">Si no solicitaste este código, puedes ignorar este correo.</p>
              </div>
            </body>
            </html>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("OTP enviado a {Email}", toEmail);
    }
}
