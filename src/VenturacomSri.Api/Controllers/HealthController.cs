using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VenturacomSri.Api.Data;

namespace VenturacomSri.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1");
            return Ok(new
            {
                status    = "ok",
                database  = "connected",
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "error",
                error  = ex.Message
            });
        }
    }
}
