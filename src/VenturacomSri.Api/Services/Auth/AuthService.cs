using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using VenturacomSri.Api.Data;
using VenturacomSri.Api.Models;

namespace VenturacomSri.Api.Services.Auth;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db     = db;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Busca o crea un usuario por correo (flujo OTP).
    /// Devuelve el usuario y si fue creado ahora.
    /// </summary>
    public async Task<(User user, bool isNew)> FindOrCreateByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);

        if (user is not null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (user, false);
        }

        user = new User
        {
            Email    = normalized,
            Provider = "email",
            Name     = normalized.Split('@')[0],
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Nuevo usuario creado por email: {Email}", normalized);
        return (user, true);
    }

    /// <summary>
    /// Valida el token de Google y crea/actualiza el usuario.
    /// </summary>
    public async Task<(User user, bool isNew)> FindOrCreateByGoogleAsync(string idToken)
    {
        var clientId = _config["Google:ClientId"]!;
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Google ID token inválido: {Msg}", ex.Message);
            throw new UnauthorizedAccessException("Token de Google inválido.");
        }

        var normalized = payload.Email.Trim().ToLowerInvariant();

        // Buscar primero por GoogleId, luego por email
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject)
                ?? await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);

        bool isNew = false;
        if (user is null)
        {
            user = new User
            {
                Email    = normalized,
                Name     = payload.Name,
                AvatarUrl = payload.Picture,
                Provider = "google",
                GoogleId = payload.Subject,
            };
            _db.Users.Add(user);
            isNew = true;
        }
        else
        {
            // Actualizar datos de perfil de Google
            user.GoogleId  = payload.Subject;
            user.Name      = payload.Name;
            user.AvatarUrl = payload.Picture;
            if (user.Provider == "email") user.Provider = "google";
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (isNew) _logger.LogInformation("Nuevo usuario creado por Google: {Email}", normalized);
        return (user, isNew);
    }
}
