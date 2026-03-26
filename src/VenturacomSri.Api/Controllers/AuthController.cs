using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VenturacomSri.Api.Data;
using VenturacomSri.Api.DTOs;
using VenturacomSri.Api.Services.Auth;

namespace VenturacomSri.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly OtpService _otp;
    private readonly EmailService _email;
    private readonly AuthService _auth;
    private readonly TokenService _tokens;
    private readonly AppDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        OtpService otp,
        EmailService email,
        AuthService auth,
        TokenService tokens,
        AppDbContext db,
        ILogger<AuthController> logger)
    {
        _otp    = otp;
        _email  = email;
        _auth   = auth;
        _tokens = tokens;
        _db     = db;
        _logger = logger;
    }

    // ── POST /api/auth/send-otp ─────────────────────────────────────────────
    /// <summary>Genera un OTP de 6 dígitos y lo envía por correo.</summary>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var code = await _otp.GenerateAsync(req.Email);
        await _email.SendOtpAsync(req.Email, code);

        return Ok(new { message = "Código enviado. Revisa tu correo." });
    }

    // ── POST /api/auth/verify-otp ───────────────────────────────────────────
    /// <summary>Verifica el OTP. Si el usuario no existe lo crea. Devuelve tokens.</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var valid = await _otp.ValidateAsync(req.Email, req.Code);
        if (!valid)
            return Unauthorized(new { error = "Código inválido o expirado." });

        var (user, isNew) = await _auth.FindOrCreateByEmailAsync(req.Email);
        var pair          = await _tokens.CreateAsync(user);

        return Ok(new AuthResponse(
            pair.AccessToken,
            pair.RefreshToken,
            pair.ExpiresAt,
            new UserDto(user.Id, user.Email, user.Name, user.AvatarUrl, user.Provider),
            isNew));
    }

    // ── POST /api/auth/google ───────────────────────────────────────────────
    /// <summary>Valida un ID token de Google. Si el usuario no existe lo crea.</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var (user, isNew) = await _auth.FindOrCreateByGoogleAsync(req.IdToken);
            var pair          = await _tokens.CreateAsync(user);

            return Ok(new AuthResponse(
                pair.AccessToken,
                pair.RefreshToken,
                pair.ExpiresAt,
                new UserDto(user.Id, user.Email, user.Name, user.AvatarUrl, user.Provider),
                isNew));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    // ── POST /api/auth/google/token ────────────────────────────────────────
    /// <summary>
    /// Valida un access token de Google (flujo initTokenClient del frontend).
    /// </summary>
    [HttpPost("google/token")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleAccessToken([FromBody] GoogleTokenRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var (user, isNew) = await _auth.FindOrCreateByGoogleAccessTokenAsync(req.AccessToken);
            var pair          = await _tokens.CreateAsync(user);

            return Ok(new AuthResponse(
                pair.AccessToken,
                pair.RefreshToken,
                pair.ExpiresAt,
                new UserDto(user.Id, user.Email, user.Name, user.AvatarUrl, user.Provider),
                isNew));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    // ── POST /api/auth/refresh ──────────────────────────────────────────────
    /// <summary>Renueva el access token usando el refresh token de Redis.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = await _tokens.ValidateRefreshAsync(req.RefreshToken);
        if (userId is null)
            return Unauthorized(new { error = "Refresh token inválido o expirado." });

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "Usuario no encontrado o inactivo." });

        // Rotar: revocar el anterior y emitir uno nuevo
        await _tokens.RevokeAsync(req.RefreshToken);
        var pair = await _tokens.CreateAsync(user);

        return Ok(new AuthResponse(
            pair.AccessToken,
            pair.RefreshToken,
            pair.ExpiresAt,
            new UserDto(user.Id, user.Email, user.Name, user.AvatarUrl, user.Provider),
            false));
    }

    // ── POST /api/auth/logout ───────────────────────────────────────────────
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
    {
        await _tokens.RevokeAsync(req.RefreshToken);
        return Ok(new { message = "Sesión cerrada." });
    }

    // ── GET /api/auth/me ────────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        return Ok(new UserDto(user.Id, user.Email, user.Name, user.AvatarUrl, user.Provider));
    }
}
