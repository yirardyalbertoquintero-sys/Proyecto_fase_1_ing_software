using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ImpactoTextil.Api.Tests;

public sealed class RegistroSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>
{
    private const string Clave = "Clave ficticia para las pruebas 2026!";

    [PruebaSql]
    public async Task PoliticaNueva_ExigeComposicionYPermiteRegistrarseEIngresarConOchoCaracteres()
    {
        using var servidor = CrearServidor();
        using var cliente = await CrearCliente(servidor);
        var correo = NuevoCorreo();
        foreach (var clave in new[] { "textil8!", "TEXTIL8!", "Textil!!", "Textil88", "Textil 8", "Tex8!" })
        {
            var solicitud = Solicitud(correo);
            solicitud["contrasena"] = clave;
            var respuesta = await cliente.PostAsJsonAsync("/api/clientes", solicitud);
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains("contrasena", await respuesta.Content.ReadAsStringAsync());
        }
        await using (var contexto = datos.CrearContexto())
            Assert.False(await contexto.Usuario.AnyAsync(u => u.correo == correo));

        var valida = Solicitud(correo);
        valida["contrasena"] = "Textil8!";
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/clientes", valida)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/sesion", new { correo, contrasena = "Textil8!" })).StatusCode);
    }

    [PruebaSql]
    public async Task RegistroValido_PersisteClienteConsentimientoYHashSinDevolverSecretos()
    {
        using var servidor = CrearServidor();
        using var cliente = await CrearCliente(servidor);
        var correo = NuevoCorreo();
        var antes = DateTimeOffset.UtcNow.AddSeconds(-1);
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", Solicitud("  " + correo.ToUpperInvariant() + "  "));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal("Cliente", json.RootElement.GetProperty("rol").GetString());
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
        Assert.DoesNotContain(Clave, cuerpo);
        Assert.DoesNotContain("passwordHash", cuerpo);
        await using var contexto = datos.CrearContexto();
        var usuario = await contexto.Usuario.SingleAsync(u => u.correo == correo);
        Assert.Equal("Persona ficticia", usuario.nombre);
        Assert.Equal(correo, usuario.correo);
        Assert.Equal("Cliente", usuario.rol);
        Assert.True(usuario.activo);
        Assert.True(usuario.aceptacionTerminos);
        Assert.InRange(usuario.fechaAceptacion!.Value, antes, DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.True(BCrypt.Net.BCrypt.Verify(Clave, usuario.passwordHash));
        Assert.Equal(usuario.id, json.RootElement.GetProperty("id").GetInt32());
    }

    [PruebaSql]
    public async Task DosRegistrosSimultaneos_MismoCorreoCreanSoloUnaCuenta()
    {
        using var servidor = CrearServidor();
        using var primero = await CrearCliente(servidor);
        using var segundo = await CrearCliente(servidor);
        var correo = NuevoCorreo();
        var respuestas = await Task.WhenAll(
            primero.PostAsJsonAsync("/api/clientes", Solicitud(correo)),
            segundo.PostAsJsonAsync("/api/clientes", Solicitud(correo.ToUpperInvariant())));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var contexto = datos.CrearContexto();
        Assert.Equal(1, await contexto.Usuario.CountAsync(u => u.correo == correo));
    }

    [PruebaSql]
    public async Task FaltaDeConsentimiento_NoPersisteCuenta()
    {
        using var servidor = CrearServidor();
        using var cliente = await CrearCliente(servidor);
        var correo = NuevoCorreo();
        var solicitud = Solicitud(correo);
        solicitud["aceptacionTerminos"] = false;
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", solicitud);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("aceptacionTerminos", await respuesta.Content.ReadAsStringAsync());
        await using var contexto = datos.CrearContexto();
        Assert.False(await contexto.Usuario.AnyAsync(u => u.correo == correo));
    }

    [PruebaSql]
    public async Task RolInyectado_EsRechazadoSinCrearCuenta()
    {
        using var servidor = CrearServidor();
        using var cliente = await CrearCliente(servidor);
        var correo = NuevoCorreo();
        var solicitud = Solicitud(correo);
        solicitud["rol"] = "Administrador";
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", solicitud);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await using var contexto = datos.CrearContexto();
        Assert.False(await contexto.Usuario.AnyAsync(u => u.correo == correo));
    }

    [PruebaSql]
    public async Task DatosInvalidosSeRechazanEnServidorSinDependerDeJavaScript()
    {
        using var servidor = CrearServidor();
        using var cliente = await CrearCliente(servidor);
        var correo = NuevoCorreo();
        var solicitud = Solicitud(correo);
        solicitud["nombre"] = "   ";
        solicitud["contrasena"] = "corta";
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", solicitud);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("nombre", cuerpo);
        Assert.Contains("contrasena", cuerpo);
        await using var contexto = datos.CrearContexto();
        Assert.False(await contexto.Usuario.AnyAsync(u => u.correo == correo));
    }

    [PruebaSql]
    public async Task ContraseñaUnicodeDeMasDe72Bytes_EsRechazadaSinTruncarse()
    {
        using var servidor = CrearServidor();
        using var cliente = await CrearCliente(servidor);
        var correo = NuevoCorreo();
        var solicitud = Solicitud(correo);
        solicitud["contrasena"] = new string('á', 37);
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", solicitud);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await using var contexto = datos.CrearContexto();
        Assert.False(await contexto.Usuario.AnyAsync(u => u.correo == correo));
    }

    [PruebaSql]
    public async Task SinTokenAntifalsificacion_NoSeAceptaElRegistro()
    {
        using var servidor = CrearServidor();
        using var cliente = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var correo = NuevoCorreo();
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", Solicitud(correo));
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("formulario_vencido", await respuesta.Content.ReadAsStringAsync());
        await using var contexto = datos.CrearContexto();
        Assert.False(await contexto.Usuario.AnyAsync(u => u.correo == correo));
    }

    [PruebaSql]
    public async Task TokenDeOtroNavegador_EsRechazadoYCookiesSonSeguras()
    {
        using var servidor = CrearServidor();
        using var primero = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var segundo = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var respuestaToken = await primero.GetAsync("/api/seguridad/token");
        var cookie = string.Join(";", respuestaToken.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.Contains("secure", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.True(respuestaToken.Headers.CacheControl?.NoStore);
        var contenido = await respuestaToken.Content.ReadFromJsonAsync<JsonElement>();
        segundo.DefaultRequestHeaders.Add("X-CSRF-TOKEN", contenido.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await segundo.PostAsJsonAsync("/api/clientes", Solicitud(NuevoCorreo()))).StatusCode);
    }

    [PruebaSql]
    public async Task MuchosIntentos_Reciben429SinCrearRegistros()
    {
        using var servidor = CrearServidor();
        using var cliente = await CrearCliente(servidor);
        for (var i = 0; i < 12; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/clientes", new { })).StatusCode);
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", Solicitud(NuevoCorreo()));
        Assert.Equal(HttpStatusCode.TooManyRequests, respuesta.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(60), respuesta.Headers.RetryAfter?.Delta);
    }

    [PruebaSql]
    public async Task PersistenciaNoDisponible_NoConfirmaCreacionNiExponeConexion()
    {
        var opciones = new SqlConnectionStringBuilder(datos.Conexion) { InitialCatalog = "NoExiste_" + Guid.NewGuid().ToString("N"), ConnectTimeout = 2 };
        using var servidor = CrearServidor(opciones.ConnectionString);
        using var cliente = await CrearCliente(servidor);
        var respuesta = await cliente.PostAsJsonAsync("/api/clientes", Solicitud(NuevoCorreo()));
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(opciones.InitialCatalog, cuerpo);
        Assert.DoesNotContain("SqlException", cuerpo);
        Assert.DoesNotContain(Clave, cuerpo);
        Assert.Contains("referencia", cuerpo);
    }

    [PruebaSql]
    public async Task Migracion_ConservaLas29ComprobacionesYNoTieneCambiosPendientes()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "ImpactoTextil.slnx"))) carpeta = carpeta.Parent;
        Assert.NotNull(carpeta);
        var sql = await File.ReadAllTextAsync(Path.Combine(carpeta.FullName, "database/pruebas_modelo_fase3.sql"), Encoding.UTF8);
        await using var conexion = new SqlConnection(datos.Conexion);
        var aprobadas = 0;
        conexion.InfoMessage += (_, evento) => aprobadas += Regex.Matches(evento.Message, "^PASS:", RegexOptions.Multiline).Count;
        await conexion.OpenAsync();
        foreach (var lote in Regex.Split(sql, @"^GO\s*$", RegexOptions.Multiline))
        {
            if (string.IsNullOrWhiteSpace(lote)) continue;
            await using var comando = new SqlCommand(lote, conexion) { CommandTimeout = 60 };
            await comando.ExecuteNonQueryAsync();
        }
        Assert.Equal(29, aprobadas);
        await using var contexto = datos.CrearContexto();
        Assert.False(contexto.Database.HasPendingModelChanges());
        await contexto.Database.MigrateAsync(); // Una segunda aplicación no recrea el esquema.
        Assert.Single(await contexto.Database.GetAppliedMigrationsAsync());
    }

    private WebApplicationFactory<Program> CrearServidor(string? conexion = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:ImpactoTextil"] = conexion ?? datos.Conexion }));
        });

    private static async Task<HttpClient> CrearCliente(WebApplicationFactory<Program> servidor)
    {
        var cliente = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var token = await cliente.GetFromJsonAsync<JsonElement>("/api/seguridad/token");
        cliente.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        return cliente;
    }

    private static string NuevoCorreo() => "prueba-" + Guid.NewGuid().ToString("N") + "@example.test";
    private static Dictionary<string, object> Solicitud(string correo) => new()
    { ["nombre"] = "  Persona ficticia  ", ["correo"] = correo, ["contrasena"] = Clave, ["aceptacionTerminos"] = true };
}
