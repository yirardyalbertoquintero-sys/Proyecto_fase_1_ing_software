using ImpactoTextil.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Controllers;

[ApiController]
[Route("api/disponibilidad")]
public sealed class DisponibilidadController(IConsultaDisponibilidad consulta) : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Consultar(CancellationToken cancelacion)
    {
        if (await consulta.ComprobarAsync(cancelacion))
            return Ok(new { estado = "Disponible" });

        return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Servicio temporalmente no disponible.");
    }
}
