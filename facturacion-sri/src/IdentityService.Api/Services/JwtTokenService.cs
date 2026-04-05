using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Api.Services;

public sealed class JwtTokenService(IConfiguration config)
{
    public string Generate(string tenantId, string username)
    {
        var secret = config["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey missing");
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: [new Claim("tenant_id", tenantId), new Claim(ClaimTypes.Name, username)],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
