using System.Globalization;
using System.Security.Claims;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImpactoTextil.Api.Controllers;

[ApiController]
[Route("api/sesion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SesionController(AccesoService acceso) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("acceso")]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Iniciar(IniciarSesionDto solicitud, CancellationToken cancelacion)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Conflict(new ProblemDetails { Status = 409, Title = "Ya tienes una sesión abierta. Ciérrala antes de cambiar de cuenta." });
        var usuario = await acceso.AutenticarAsync(solicitud, cancelacion);
        if (usuario is null)
            return Unauthorized(new ProblemDetails { Status = 401, Title = "El correo o la contraseña no son válidos, o la cuenta no está disponible." });
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            ValidarSesion.Identidad(usuario), new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
        return Ok(new SesionDto(usuario.id, usuario.nombre, usuario.rol));
    }

    [HttpGet]
    [Authorize]
    public ActionResult<SesionDto> Actual() => new SesionDto(
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture),
        User.Identity!.Name!, User.FindFirstValue(ClaimTypes.Role)!);

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Cerrar()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
