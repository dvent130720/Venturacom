using Microsoft.AspNetCore.Mvc;
using VenturacomApi.Models;
using VenturacomApi.Services;

namespace VenturacomApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IOtpService _otp;
    private readonly IEmailService _email;
    private readonly IJwtService _jwt;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IOtpService otp, IEmailService email, IJwtService jwt, ILogger<AuthController> logger)
    {
        _otp = otp;
        _email = email;
        _jwt = jwt;
        _logger = logger;
    }

    /// <summary>Sends a one-time password to the given email address.</summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return BadRequest(new { message = "Correo electrónico inválido." });

        var code = _otp.GenerateAndStore(req.Email);

        try
        {
            await _email.SendOtpAsync(req.Email, code, ct);
        }
        catch
        {
            // In development we still return success – OTP is logged to console
            _logger.LogWarning("Email service failed. DEV OTP for {Email}: {Code}", req.Email, code);
        }

        return Ok(new { message = "Código enviado. Revisa tu correo." });
    }

    /// <summary>Verifies an OTP and returns a JWT on success.</summary>
    [HttpPost("verify-otp")]
    public IActionResult VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        if (!_otp.Verify(req.Email, req.Otp))
            return BadRequest(new { message = "Código incorrecto o expirado." });

        var user = new AuthUser(
            Id: Guid.NewGuid().ToString(),
            Email: req.Email,
            Name: req.Email.Split('@')[0],
            PhotoUrl: null,
            Provider: "email"
        );

        var token = _jwt.Generate(user);
        return Ok(new AuthResponse(token, user));
    }

    /// <summary>Google OAuth redirect – handled by ASP.NET Core middleware.</summary>
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth");
        var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(props, "Google");
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("Google");
        if (!result.Succeeded) return Redirect("/?error=google_failed");

        var claims = result.Principal!.Claims.ToList();
        var email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
        var name = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
        var photo = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

        var user = new AuthUser(Guid.NewGuid().ToString(), email, name, photo, "google");
        var token = _jwt.Generate(user);

        // Redirect to Angular with token in fragment
        return Redirect($"http://localhost:4200/?token={token}");
    }
}
