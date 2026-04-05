using IdentityService.Api.Contracts;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(JwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("token")]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        // Demo validation; replace with ASP.NET Identity / external IdP.
        if (request.Password != "ChangeMe!") return Unauthorized();

        var token = jwtTokenService.Generate(request.TenantId, request.Username);
        return Ok(new { access_token = token, token_type = "Bearer", expires_in = 28800 });
    }
}
