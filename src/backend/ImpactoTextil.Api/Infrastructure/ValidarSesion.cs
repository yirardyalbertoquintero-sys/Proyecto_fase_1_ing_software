using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ImpactoTextil.Api.Infrastructure;

public sealed class ValidarSesion(IUsuarioRepository usuarios) : CookieAuthenticationEvents
{
    public static string VersionCredencial(Usuario usuario) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(usuario.passwordHash)));

    public static ClaimsPrincipal Identidad(Usuario usuario) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString(CultureInfo.InvariantCulture)),
        new Claim(ClaimTypes.Name, usuario.nombre),
        new Claim(ClaimTypes.Role, usuario.rol),
        new Claim("versionCredencial", VersionCredencial(usuario))
    ], CookieAuthenticationDefaults.AuthenticationScheme));

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var usuario = int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? await usuarios.BuscarPorIdAsync(id, context.HttpContext.RequestAborted) : null;
        if (usuario is not { activo: true, rol: "Cliente" or "Administrador" }
            || principal?.FindFirstValue(ClaimTypes.Role) != usuario.rol
            || principal.FindFirstValue("versionCredencial") != VersionCredencial(usuario))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        context.ReplacePrincipal(Identidad(usuario));
        context.ShouldRenew = true;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) =>
        Denegar(context.HttpContext, 401, "Inicia sesión para continuar.");

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) =>
        Denegar(context.HttpContext, 403, "No tienes permiso para realizar esta operación.");

    private static Task Denegar(HttpContext contexto, int estado, string titulo)
    {
        contexto.Response.StatusCode = estado;
        contexto.Response.Headers.CacheControl = "no-store";
        return contexto.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        { Status = estado, Title = titulo });
    }
}
