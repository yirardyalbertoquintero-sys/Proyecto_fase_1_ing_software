using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Controllers;

[ApiController]
[Route("api/seguridad")]
public sealed class SeguridadController(IAntiforgery antifalsificacion) : ControllerBase
{
    [HttpGet("token")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ObtenerToken() => Ok(new { token = antifalsificacion.GetAndStoreTokens(HttpContext).RequestToken });
}
