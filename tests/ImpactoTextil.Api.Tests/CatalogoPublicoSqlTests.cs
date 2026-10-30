using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ImpactoTextil.Api.Tests;

public sealed class CatalogoPublicoSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>
{
    [PruebaSql]
    public async Task ListadoPublico_DevuelveSoloActivosSinCamposAdministrativosNiPerdidaDeCentavos()
    {
        var activo = await Producto(activo: true, precio: 9999999999999999.99m);
        var inactivo = await Producto(activo: false);
        using var servidor = Servidor(); using var cliente = Cliente(servidor);
        var respuesta = await cliente.GetAsync("/api/productos");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl?.NoStore);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var filas = json.EnumerateArray().ToArray();
        Assert.DoesNotContain(filas, p => p.GetProperty("id").GetInt32() == inactivo.id);
        var fila = Assert.Single(filas, p => p.GetProperty("id").GetInt32() == activo.id);
        Assert.Equal("9999999999999999.99", fila.GetProperty("precioBase").GetString());
        Assert.Equal(new[] { "color", "id", "imagenURL", "nombre", "precioBase", "talla", "tela" },
            fila.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(activo.nombre, fila.GetProperty("nombre").GetString());
        Assert.Equal(activo.talla, fila.GetProperty("talla").GetString());
    }

    [PruebaSql]
    public async Task DetalleReconsultaDisponibilidadYPrecio_NoOfreceUnaVarianteDesactivada()
    {
        var producto = await Producto();
        using var servidor = Servidor(); using var cliente = Cliente(servidor);
        var ruta = $"/api/productos/{producto.id}";
        Assert.Equal(150.25m, (await cliente.GetFromJsonAsync<ProductoPublicoDto>(ruta))!.precioBase);
        await using (var contexto = datos.CrearContexto())
        {
            var actual = await contexto.Producto.SingleAsync(p => p.id == producto.id);
            actual.precioBase = 210.90m; await contexto.SaveChangesAsync();
        }
        var actualizada = await cliente.GetAsync(ruta);
        Assert.True(actualizada.Headers.CacheControl?.NoStore);
        Assert.Equal(210.90m, (await actualizada.Content.ReadFromJsonAsync<ProductoPublicoDto>())!.precioBase);
        await using (var contexto = datos.CrearContexto())
        {
            var actual = await contexto.Producto.SingleAsync(p => p.id == producto.id);
            actual.activo = false; await contexto.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync(ruta)).StatusCode);
        Assert.DoesNotContain((await cliente.GetFromJsonAsync<ProductoPublicoDto[]>("/api/productos"))!, p => p.id == producto.id);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/productos/2147483647")).StatusCode);
    }

    [PruebaSql]
    public async Task VisitanteClienteYAdministrador_PuedenConsultarSinConcederAdministracionPublica()
    {
        const string clave = "contraseña ficticia de consulta";
        var producto = await Producto();
        using var servidor = Servidor();
        foreach (var rol in new[] { "Visitante", "Cliente", "Administrador" })
        {
            using var cliente = Cliente(servidor);
            if (rol != "Visitante")
            {
                var correo = Guid.NewGuid().ToString("N") + "@example.test";
                await using (var contexto = datos.CrearContexto())
                {
                    contexto.Usuario.Add(new Usuario { nombre = "Persona ficticia", correo = correo, rol = rol, activo = true,
                        passwordHash = new ProtectorContrasena().CrearHash(clave), aceptacionTerminos = true, fechaAceptacion = DateTimeOffset.UtcNow });
                    await contexto.SaveChangesAsync();
                }
                var token = await cliente.GetFromJsonAsync<JsonElement>("/api/seguridad/token");
                cliente.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
                Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/sesion", new { correo, contrasena = clave })).StatusCode);
            }
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/productos")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/productos/{producto.id}")).StatusCode);
            if (rol == "Visitante") Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/administracion/productos")).StatusCode);
            if (rol == "Cliente") Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/administracion/productos")).StatusCode);
        }
    }

    [PruebaSql]
    public async Task ReferenciasAnterioresPrivadasOExternas_NoSeExponenPorElCatalogoPublico()
    {
        using var servidor = Servidor(); using var cliente = Cliente(servidor);
        foreach (var referencia in new[] { "/privado/diseno.png", "https://example.test/rastreo.png", "C:\\privado\\comprobante.png" })
        {
            var producto = await Producto(imagen: referencia);
            var respuesta = await cliente.GetFromJsonAsync<ProductoPublicoDto>($"/api/productos/{producto.id}");
            Assert.Null(respuesta!.imagenURL);
            await using var contexto = datos.CrearContexto();
            Assert.Equal(referencia, (await contexto.Producto.SingleAsync(p => p.id == producto.id)).imagenURL);
        }
    }

    [PruebaSql]
    public async Task RutasPublicas_NoAdmitenEscriturasNiBorrado()
    {
        var producto = await Producto();
        using var servidor = Servidor(); using var cliente = Cliente(servidor);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.PostAsJsonAsync("/api/productos", new { nombre = "No guardar" })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.PutAsJsonAsync($"/api/productos/{producto.id}", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.DeleteAsync($"/api/productos/{producto.id}")).StatusCode);
        await using var contexto = datos.CrearContexto();
        Assert.True((await contexto.Producto.SingleAsync(p => p.id == producto.id)).activo);
    }

    [PruebaSql]
    public async Task FalloDeConsulta_NoSimulaUnCatalogoVacioNiExponeElServidor()
    {
        using var servidor = Servidor("Server=127.0.0.1,1;Database=inexistente;User Id=ficticio;Password=no-es-real;Encrypt=True;TrustServerCertificate=True;Connect Timeout=1;ConnectRetryCount=0");
        using var cliente = Cliente(servidor);
        foreach (var ruta in new[] { "/api/productos", "/api/productos/1" })
        {
            var respuesta = await cliente.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            Assert.DoesNotContain("SqlException", cuerpo); Assert.DoesNotContain("127.0.0.1", cuerpo);
            Assert.DoesNotContain("no-es-real", cuerpo); Assert.Contains("referencia", cuerpo);
        }
    }

    private async Task<Producto> Producto(bool activo = true, decimal precio = 150.25m, string? imagen = null)
    {
        var producto = new Producto { nombre = "Prenda ficticia " + Guid.NewGuid().ToString("N"), color = "Blanco", talla = "M", tela = "Poliéster",
            imagenURL = imagen ?? "/api/imagenes-catalogo/" + Guid.NewGuid().ToString("N") + ".png", precioBase = precio, activo = activo };
        await using var contexto = datos.CrearContexto(); contexto.Producto.Add(producto); await contexto.SaveChangesAsync(); return producto;
    }

    private WebApplicationFactory<Program> Servidor(string? conexion = null) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?>
        { ["ConnectionStrings:ImpactoTextil"] = conexion ?? datos.Conexion }));
    });

    private static HttpClient Cliente(WebApplicationFactory<Program> servidor) => servidor.CreateClient(new()
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
}

public sealed class CatalogoPublicoVacioSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>
{
    [PruebaSql]
    public async Task SinProductos_RespondeListadoVacioRealConEstado200()
    {
        using var servidor = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?>
            { ["ConnectionStrings:ImpactoTextil"] = datos.Conexion }));
        });
        using var cliente = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var respuesta = await cliente.GetAsync("/api/productos");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty((await respuesta.Content.ReadFromJsonAsync<ProductoPublicoDto[]>())!);
    }
}
