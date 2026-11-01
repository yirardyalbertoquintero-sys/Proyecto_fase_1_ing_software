using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImpactoTextil.Api.Tests;

// Estos recursos existen únicamente en el servidor de pruebas, nunca en la aplicación publicada.
[ApiController]
[Route("api/pruebas-permisos")]
public sealed class PermisosPruebaController : ControllerBase
{
    [HttpGet("administrador"), Authorize(Policy = "Administrador")]
    public IActionResult Administrador() => Ok();
    [HttpGet("cliente"), Authorize(Policy = "Cliente")]
    public IActionResult Cliente() => Ok();
}

public sealed class SesionSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>
{
    private const string Clave = "  frase ficticia de acceso  ";

    [PruebaSql]
    public async Task AccesoNormalizaCorreo_CookieSeguraNoPersistente_SinSecretos()
    {
        var usuario = await CrearUsuario();
        using var servidor = Servidor();
        using var cliente = Cliente(servidor);
        var respuesta = await Entrar(cliente, usuario.correo.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cookie = CookieSesion(respuesta);
        Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        Assert.DoesNotContain("expires=", cookie.ToLowerInvariant());
        Assert.DoesNotContain("max-age=", cookie.ToLowerInvariant());
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", cuerpo);
        Assert.DoesNotContain(usuario.passwordHash, cuerpo);
        Assert.DoesNotContain("versionCredencial", cuerpo);
        Assert.True(respuesta.Headers.CacheControl?.NoStore);
        var sesion = await cliente.GetFromJsonAsync<SesionDto>("/api/sesion");
        Assert.Equal(usuario.id, sesion!.id);
        Assert.Equal("Cliente", sesion.rol);
        Assert.Equal(usuario.nombre, sesion.nombre);
    }

    [PruebaSql]
    public async Task ClaveIncorrectaCorreoAusenteEInactivo_MismaDenegacionSinSesion()
    {
        var activo = await CrearUsuario();
        var inactivo = await CrearUsuario(activo: false);
        using var servidor = Servidor();
        string? mensaje = null;
        foreach (var (correo, clave) in new[] { (activo.correo, "otra contraseña"), ("ausente@example.test", Clave), (inactivo.correo, Clave), (activo.correo, Clave.Trim()) })
        {
            using var cliente = Cliente(servidor);
            var respuesta = await Entrar(cliente, correo, clave);
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            mensaje ??= cuerpo;
            Assert.Equal(mensaje, cuerpo);
            Assert.False(respuesta.Headers.TryGetValues("Set-Cookie", out var cookies)
                && cookies.Any(c => c.StartsWith("__Host-ImpactoTextil.Sesion=")));
            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/sesion")).StatusCode);
        }
    }

    [PruebaSql]
    public async Task PermisosSeTomanDeLaCuenta_ClienteNoAdminYViceversa()
    {
        using var servidor = Servidor();
        foreach (var rol in new[] { "Cliente", "Administrador" })
        {
            var usuario = await CrearUsuario(rol);
            using var cliente = Cliente(servidor);
            Assert.Equal(HttpStatusCode.OK, (await Entrar(cliente, usuario.correo)).StatusCode);
            var permitido = rol == "Cliente" ? "cliente" : "administrador";
            var denegado = rol == "Cliente" ? "administrador" : "cliente";
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/pruebas-permisos/" + permitido)).StatusCode);
            var respuesta = await cliente.GetAsync("/api/pruebas-permisos/" + denegado);
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Null(respuesta.Headers.Location);
        }
        using var anonimo = Cliente(servidor);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/pruebas-permisos/administrador")).StatusCode);
    }

    [PruebaSql]
    public async Task DesactivarCambiarRolOClave_InvalidaLaSesionYaAbierta()
    {
        using var servidor = Servidor();
        for (var cambio = 0; cambio < 3; cambio++)
        {
            var usuario = await CrearUsuario();
            using var cliente = Cliente(servidor);
            Assert.Equal(HttpStatusCode.OK, (await Entrar(cliente, usuario.correo)).StatusCode);
            await using var contexto = datos.CrearContexto();
            var entidad = await contexto.Usuario.SingleAsync(u => u.id == usuario.id);
            if (cambio == 0) entidad.activo = false;
            if (cambio == 1) entidad.rol = "Administrador";
            if (cambio == 2) entidad.passwordHash = new ProtectorContrasena().CrearHash("otra contraseña de pruebas");
            await contexto.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/sesion")).StatusCode);
        }
    }

    [PruebaSql]
    public async Task CerrarSesion_ExigeTokenYRevocaInclusoLaCookieAnterior()
    {
        var usuario = await CrearUsuario();
        using var servidor = Servidor();
        using var cliente = Cliente(servidor);
        var entrada = await Entrar(cliente, usuario.correo);
        var cookie = CookieSesion(entrada).Split(';')[0];
        cliente.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.DeleteAsync("/api/sesion")).StatusCode);
        await Token(cliente);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync("/api/sesion")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/sesion")).StatusCode);
        using var repetidor = Cliente(servidor, cookies: false);
        repetidor.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await repetidor.GetAsync("/api/sesion")).StatusCode);
        // Volver a entrar después del cierre exige un token de la identidad anónima.
        Assert.Equal(HttpStatusCode.OK, (await Entrar(cliente, usuario.correo)).StatusCode);
    }

    [PruebaSql]
    public async Task SesionExpiraTrasDiezMinutosSinUso_YLaActividadRenuevaElPlazo()
    {
        var usuario = await CrearUsuario();
        var reloj = new RelojPruebas();
        using var servidor = Servidor(reloj: reloj);
        using var cliente = Cliente(servidor);
        Assert.Equal(HttpStatusCode.OK, (await Entrar(cliente, usuario.correo)).StatusCode);
        reloj.Avanzar(TimeSpan.FromMinutes(9));
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/sesion")).StatusCode);
        reloj.Avanzar(TimeSpan.FromMinutes(9));
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/sesion")).StatusCode);
        reloj.Avanzar(TimeSpan.FromMinutes(10).Add(TimeSpan.FromSeconds(1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/sesion")).StatusCode);
    }

    [PruebaSql]
    public async Task AccesoSinTokenODatosInyectados_NoCreaSesion()
    {
        var usuario = await CrearUsuario();
        using var servidor = Servidor();
        using var cliente = Cliente(servidor);
        var solicitud = new { correo = usuario.correo, contrasena = Clave };
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/sesion", solicitud)).StatusCode);
        await Token(cliente);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/sesion", new { usuario.correo, contrasena = Clave, rol = "Administrador" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/sesion", new { correo = "", contrasena = new string('á', 37) })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/sesion")).StatusCode);
    }

    [PruebaSql]
    public async Task CookieManipulada_NoAutentica_YNoSePuedeCambiarCuentaConSesionAbierta()
    {
        var usuario = await CrearUsuario();
        using var servidor = Servidor();
        using var cliente = Cliente(servidor);
        var entrada = await Entrar(cliente, usuario.correo);
        var cookie = CookieSesion(entrada).Split(';')[0];
        using var manipulado = Cliente(servidor, cookies: false);
        manipulado.DefaultRequestHeaders.Add("Cookie", cookie + "alterada");
        Assert.Equal(HttpStatusCode.Unauthorized, (await manipulado.GetAsync("/api/sesion")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Entrar(cliente, usuario.correo)).StatusCode);
    }

    [PruebaSql]
    public async Task FalloSql_NoCreaSesionNiExponeErroresInternos()
    {
        using var servidor = Servidor(conexion: "Server=127.0.0.1,1;Database=inexistente;User Id=ficticio;Password=no-es-real;Encrypt=True;TrustServerCertificate=True;Connect Timeout=1;ConnectRetryCount=0");
        using var cliente = Cliente(servidor);
        var respuesta = await Entrar(cliente, "persona@example.test");
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SqlException", cuerpo);
        Assert.DoesNotContain("no-es-real", cuerpo);
        Assert.DoesNotContain("127.0.0.1", cuerpo);
        Assert.Contains("referencia", cuerpo);
        Assert.False(respuesta.Headers.Contains("Set-Cookie"));
    }

    [PruebaSql]
    public async Task IntentosExcesivos_Responden429SinBloquearLaCuenta()
    {
        using var servidor = Servidor();
        using var cliente = Cliente(servidor);
        await Token(cliente);
        for (var i = 0; i < 12; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/sesion", new { correo = "", contrasena = "" })).StatusCode);
        var respuesta = await cliente.PostAsJsonAsync("/api/sesion", new { correo = "", contrasena = "" });
        Assert.Equal(HttpStatusCode.TooManyRequests, respuesta.StatusCode);
        Assert.Equal("60", respuesta.Headers.GetValues("Retry-After").Single());
    }

    private async Task<Usuario> CrearUsuario(string rol = "Cliente", bool activo = true)
    {
        var usuario = new Usuario { nombre = "Persona ficticia", correo = Guid.NewGuid().ToString("N") + "@example.test",
            passwordHash = new ProtectorContrasena().CrearHash(Clave), rol = rol, activo = activo,
            aceptacionTerminos = true, fechaAceptacion = DateTimeOffset.UtcNow };
        await using var contexto = datos.CrearContexto();
        contexto.Usuario.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario;
    }

    private WebApplicationFactory<Program> Servidor(TimeProvider? reloj = null, string? conexion = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:ImpactoTextil"] = conexion ?? datos.Conexion, ["Sesion:MinutosInactividad"] = "10" }));
            builder.ConfigureServices(servicios =>
            {
                servicios.AddControllers().AddApplicationPart(typeof(PermisosPruebaController).Assembly);
                if (reloj is not null) { servicios.RemoveAll<TimeProvider>(); servicios.AddSingleton(reloj); }
            });
        });

    private static HttpClient Cliente(WebApplicationFactory<Program> servidor, bool cookies = true) =>
        servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = cookies });

    private static async Task Token(HttpClient cliente)
    {
        var cuerpo = await cliente.GetFromJsonAsync<JsonElement>("/api/seguridad/token");
        cliente.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        cliente.DefaultRequestHeaders.Add("X-CSRF-TOKEN", cuerpo.GetProperty("token").GetString());
    }

    private static async Task<HttpResponseMessage> Entrar(HttpClient cliente, string correo, string clave = Clave)
    {
        await Token(cliente);
        return await cliente.PostAsJsonAsync("/api/sesion", new { correo = " " + correo + " ", contrasena = clave });
    }

    private static string CookieSesion(HttpResponseMessage respuesta) =>
        respuesta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("__Host-ImpactoTextil.Sesion="));
}

public sealed class RelojPruebas : TimeProvider
{
    private DateTimeOffset ahora = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => ahora;
    public void Avanzar(TimeSpan intervalo) => ahora += intervalo;
}
