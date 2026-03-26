using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using VenturacomSri.Api.Models;

namespace VenturacomSri.Api.Services.Auth;

public class TokenService
{
    private readonly IConfiguration _config;
    private readonly IDatabase _redis;
    private const string SessionPrefix = "session:";

    public TokenService(IConfiguration config, IConnectionMultiplexer redis)
    {
        _config = config;
        _redis  = redis.GetDatabase();
    }

    /// <summary>Genera un JWT de acceso (15 min) y un refresh token (30 días) en Redis.</summary>
    public async Task<TokenPair> CreateAsync(User user)
    {
        var jwtSection    = _config.GetSection("Jwt");
        var secret        = jwtSection["Secret"]!;
        var issuer        = jwtSection["Issuer"]!;
        var audience      = jwtSection["Audience"]!;
        var accessMinutes = int.Parse(jwtSection["AccessTokenMinutes"] ?? "15");
        var refreshDays   = int.Parse(jwtSection["RefreshTokenDays"]   ?? "30");

        // ── Access token ────────────────────────────────────────────────
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name ?? user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now   = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          now,
            expires:            now.AddMinutes(accessMinutes),
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // ── Refresh token (opaco) en Redis ──────────────────────────────
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var redisKey     = SessionPrefix + refreshToken;
        await _redis.StringSetAsync(redisKey, user.Id.ToString(), TimeSpan.FromDays(refreshDays));

        return new TokenPair(accessToken, refreshToken, now.AddMinutes(accessMinutes));
    }

    /// <summary>Valida un refresh token y devuelve el UserId si es válido.</summary>
    public async Task<Guid?> ValidateRefreshAsync(string refreshToken)
    {
        var redisKey = SessionPrefix + refreshToken;
        var value    = await _redis.StringGetAsync(redisKey);
        if (value.IsNullOrEmpty) return null;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>Revoca un refresh token (logout).</summary>
    public async Task RevokeAsync(string refreshToken)
    {
        await _redis.KeyDeleteAsync(SessionPrefix + refreshToken);
    }
}

public record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAt);
