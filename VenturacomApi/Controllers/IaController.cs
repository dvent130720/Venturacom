using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VenturacomApi.Controllers;

public record ChatRequest(string Message);
public record ChatResponse(string Reply);

[ApiController]
[Route("api/ia")]
public class IaController : ControllerBase
{
    // Placeholder: connect to OpenAI / Azure AI or your preferred LLM here.
    private static readonly string[] _demos =
    [
        "Basado en los datos del sistema, este mes las ventas han aumentado un 12% respecto al mes anterior.",
        "Los productos con mayor demanda son Laptops HP ProBook y Monitores Dell 27\".",
        "Tu balance neto del mes es positivo: $97,200 en ingresos netos.",
        "Te recomiendo enfocarte en los clientes con órdenes pendientes y activar campañas de seguimiento.",
        "El sector de redes empresariales muestra una tendencia de crecimiento del 18% para el próximo trimestre.",
    ];

    [HttpPost("chat")]
    public IActionResult Chat([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { message = "El mensaje no puede estar vacío." });

        // Replace with actual LLM call:
        var reply = _demos[Random.Shared.Next(_demos.Length)];
        return Ok(new ChatResponse(reply));
    }
}
