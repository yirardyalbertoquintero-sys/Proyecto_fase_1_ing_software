using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImpactoTextil.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(RegistroClienteService registro) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("registro")]
    [RequestSizeLimit(16_384)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Registrar(RegistrarClienteDto solicitud, CancellationToken cancelacion)
    {
        var creado = await registro.RegistrarAsync(solicitud, cancelacion);
        if (creado is null)
            return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["correo"] = ["Ya existe una cuenta con ese correo."]
            }) { Status = 409, Title = "Revisa el correo ingresado." });

        return StatusCode(StatusCodes.Status201Created, creado);
    }
}
