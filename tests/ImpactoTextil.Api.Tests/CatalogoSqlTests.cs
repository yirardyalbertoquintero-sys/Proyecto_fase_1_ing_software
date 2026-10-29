using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ImpactoTextil.Api.Tests;

public sealed class CatalogoSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>, IDisposable
{
    private const string Clave = "contraseña ficticia del catálogo";
    public static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAAAE0lEQVR4nGP88PoOAwwwwVl4OQCI1gK/rni6WQAAAABJRU5ErkJggg==");
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "ImpactoTextil_CatalogoPruebas_" + Guid.NewGuid().ToString("N"));

    [PruebaSql]
    public async Task PreciosEnteros_ConservanVersionValidaAlVolverDeSql()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        var imagen = await SubirImagen(admin);
        var respuesta = await admin.PostAsJsonAsync("/api/administracion/productos", Datos(imagen, precio: "150"));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var producto = (await respuesta.Content.ReadFromJsonAsync<ProductoDto>())!;
        foreach (var precio in new[] { "210", "220.00" })
        {
            var consulta = await admin.GetFromJsonAsync<ProductoDto>($"/api/administracion/productos/{producto.id}");
            Assert.Equal(producto.version, consulta!.version);
            var edicion = await admin.PutAsJsonAsync($"/api/administracion/productos/{producto.id}", Datos(imagen, producto.version, precio: precio));
            Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
            producto = (await edicion.Content.ReadFromJsonAsync<ProductoDto>())!;
        }
    }

    [PruebaSql]
    public async Task ImagenJpegReal_SeValidaYGuardaConNombreGenerado()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        var bytes = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAAEAAQDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD2uiiisDQ//9k=");
        using var formulario = Formulario(bytes, "nombre-del-usuario.JPEG");
        var respuesta = await admin.PostAsync("/api/imagenes-catalogo", formulario);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var referencia = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imagenURL").GetString()!;
        Assert.Matches("^/api/imagenes-catalogo/[a-f0-9]{32}\\.jpg$", referencia);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Directory.GetFiles(carpeta).Single()));
    }

    [PruebaSql]
    public async Task Administrador_CreaConsultaEditaYDesactiva_SinEliminarLaVariante()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        var imagen = await SubirImagen(admin);
        var creado = await CrearProducto(admin, imagen);
        Assert.True(creado.activo);
        Assert.Equal(150.25m, creado.precioBase);
        var lectura = await admin.GetFromJsonAsync<ProductoDto>($"/api/administracion/productos/{creado.id}");
        Assert.Equal(creado, lectura);
        var respuesta = await admin.PutAsJsonAsync($"/api/administracion/productos/{creado.id}", Datos(imagen, creado.version, false, "200.50"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var actualizado = (await respuesta.Content.ReadFromJsonAsync<ProductoDto>())!;
        Assert.False(actualizado.activo);
        Assert.Equal(200.50m, actualizado.precioBase);
        Assert.NotEqual(creado.version, actualizado.version);
        Assert.Contains((await admin.GetFromJsonAsync<ProductoDto[]>("/api/administracion/productos"))!, p => p.id == creado.id && !p.activo);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await admin.DeleteAsync($"/api/administracion/productos/{creado.id}")).StatusCode);
        await using var contexto = datos.CrearContexto();
        Assert.True(await contexto.Producto.AnyAsync(p => p.id == creado.id));
    }

    [PruebaSql]
    public async Task VisitanteYCliente_NoAdministranNiSubenImagenes()
    {
        using var servidor = Servidor();
        using var visitante = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var cliente = await Cliente(servidor, "Cliente");
        foreach (var (actor, estado) in new[] { (visitante, HttpStatusCode.Unauthorized), (cliente, HttpStatusCode.Forbidden) })
        {
            Assert.Equal(estado, (await actor.GetAsync("/api/administracion/productos")).StatusCode);
            await Token(actor);
            Assert.Equal(estado, (await actor.PostAsJsonAsync("/api/administracion/productos", Datos("/privado/diseno.png"))).StatusCode);
            using var archivo = Formulario(Png, "prenda.png");
            Assert.Equal(estado, (await actor.PostAsync("/api/imagenes-catalogo", archivo)).StatusCode);
        }
        Assert.False(Directory.Exists(carpeta));
    }

    [PruebaSql]
    public async Task ImagenDisponibleSoloSiEsPublicaYReferenciada_NoAceptaRutasPrivadasOExternas()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        var imagen = await SubirImagen(admin);
        using var publico = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await publico.GetAsync(imagen)).StatusCode);
        foreach (var referencia in new[] { "/privado/diseno.png", "https://example.test/prenda.png", "/api/imagenes-catalogo/../../secreto.png" })
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/productos", Datos(referencia))).StatusCode);
        await CrearProducto(admin, imagen);
        var respuesta = await publico.GetAsync(imagen);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("image/png", respuesta.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Png, await respuesta.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [PruebaSql]
    public async Task ImagenFalsaDanadaYFormatoEquivocado_SeRechazanSinGuardar()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        foreach (var (contenido, nombre) in new[] { (Encoding.UTF8.GetBytes("<script>no es imagen</script>"), "falsa.png"),
            (Png, "falsa.jpg"), (Png[..24], "danada.png"), (Png, "archivo.svg"), (Array.Empty<byte>(), "vacio.png") })
        {
            await Token(admin);
            using var formulario = Formulario(contenido, nombre);
            var respuesta = await admin.PostAsync("/api/imagenes-catalogo", formulario);
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }
        Assert.False(Directory.Exists(carpeta));
    }

    [PruebaSql]
    public async Task ImagenRespetaLimiteInclusivoDeDiezMillonesDeBytes()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        var contenido = new byte[ImagenCatalogoStore.MaximoBytes];
        Png.CopyTo(contenido, 0);
        await Token(admin);
        using var valida = Formulario(contenido, "limite.png");
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync("/api/imagenes-catalogo", valida)).StatusCode);
        await Token(admin);
        using var excesiva = Formulario(new byte[ImagenCatalogoStore.MaximoBytes + 1], "excesiva.png");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/imagenes-catalogo", excesiva)).StatusCode);
        Assert.Single(Directory.GetFiles(carpeta));
    }

    [PruebaSql]
    public async Task CamposInvalidosYSinCsrf_NoCreanProducto()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        var imagen = await SubirImagen(admin);
        admin.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/productos", Datos(imagen))).StatusCode);
        await Token(admin);
        foreach (var precio in new[] { "0", "-1", "150.251", "10000000000000000", "NaN" })
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/productos", Datos(imagen, precio: precio))).StatusCode);
        var campos = Datos(imagen); campos["nombre"] = " "; campos["id"] = 88;
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/productos", campos)).StatusCode);
        using var sinArchivo = Formulario(Png, "sin-token.png");
        admin.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/imagenes-catalogo", sinArchivo)).StatusCode);
        await using var contexto = datos.CrearContexto();
        Assert.False(await contexto.Producto.AnyAsync(p => p.imagenURL == imagen));
    }

    [PruebaSql]
    public async Task EdicionConcurrente_ConservaUnSoloCambioYRechazaVersionObsoleta()
    {
        using var servidor = Servidor();
        using var primero = await Cliente(servidor, "Administrador");
        using var segundo = await Cliente(servidor, "Administrador");
        var imagen = await SubirImagen(primero);
        var producto = await CrearProducto(primero, imagen);
        await Token(primero); await Token(segundo);
        var respuestas = await Task.WhenAll(
            primero.PutAsJsonAsync($"/api/administracion/productos/{producto.id}", Datos(imagen, producto.version, precio: "210")),
            segundo.PutAsJsonAsync($"/api/administracion/productos/{producto.id}", Datos(imagen, producto.version, precio: "220")));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        var actual = await primero.GetFromJsonAsync<ProductoDto>($"/api/administracion/productos/{producto.id}");
        Assert.Contains(actual!.precioBase, new[] { 210m, 220m });
        Assert.Equal(HttpStatusCode.BadRequest, (await primero.PutAsJsonAsync($"/api/administracion/productos/{producto.id}", Datos(imagen))).StatusCode);
    }

    [PruebaSql]
    public async Task CambiarCatalogoNoReescribeLaConfiguracionNiImportesDelPedido()
    {
        using var servidor = Servidor();
        using var admin = await Cliente(servidor, "Administrador");
        var imagen = await SubirImagen(admin);
        var producto = await CrearProducto(admin, imagen);
        int detalleId;
        int versionId;
        await using (var contexto = datos.CrearContexto())
        {
            var usuario = await contexto.Usuario.FirstAsync();
            var pedido = new Pedido { codigo = Guid.NewGuid().ToString("N"), cliente = usuario.id, fechaOrden = DateTimeOffset.UtcNow,
                estado = "Pendiente", condicionAdministrativa = "Activa", modalidadEntrega = "Retiro", confirmacion = Guid.NewGuid(), huellaSolicitud = new byte[32] };
            contexto.Pedido.Add(pedido); await contexto.SaveChangesAsync();
            var detalle = new DetallePedido { pedido = pedido.id, producto = producto.id, cantidad = 2, talla = "M", color = "Blanco",
                tela = "Poliéster", empaque = "Sin empaque", ideaDiseno = "Sin personalización", precioBaseReferencia = producto.precioBase };
            contexto.DetallePedido.Add(detalle); await contexto.SaveChangesAsync(); detalleId = detalle.id;
            var cotizacion = new VersionCotizacion { pedido = pedido.id, numeroVersion = 1, fechaRegistro = DateTimeOffset.UtcNow,
                responsableRegistro = usuario.id, decision = "Propuesta" };
            contexto.VersionCotizacion.Add(cotizacion); await contexto.SaveChangesAsync(); versionId = cotizacion.id;
            contexto.DetalleCotizacion.Add(new DetalleCotizacion { versionCotizacion = versionId, detalle = detalleId, precioUnitarioAcordado = 175m });
            await contexto.SaveChangesAsync();
            cotizacion.decision = "Aceptada"; cotizacion.fechaDecision = cotizacion.fechaRegistro.AddMinutes(1);
            cotizacion.responsableDecision = usuario.id; cotizacion.referenciaDecision = "Constancia ficticia de aceptación";
            await contexto.SaveChangesAsync();
            pedido.versionCotizacionVigente = versionId; await contexto.SaveChangesAsync();
        }
        var nuevos = Datos(imagen, producto.version, false, "999.99"); nuevos["color"] = "Negro"; nuevos["talla"] = "L";
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/productos/{producto.id}", nuevos)).StatusCode);
        await using var verificacion = datos.CrearContexto();
        var guardado = await verificacion.DetallePedido.SingleAsync(d => d.id == detalleId);
        Assert.Equal("M", guardado.talla); Assert.Equal("Blanco", guardado.color);
        Assert.Equal(150.25m, guardado.precioBaseReferencia);
        Assert.Equal(175m, (await verificacion.DetalleCotizacion.SingleAsync(d => d.versionCotizacion == versionId && d.detalle == detalleId)).precioUnitarioAcordado);
        Assert.False((await verificacion.Producto.SingleAsync(p => p.id == producto.id)).activo);
    }

    private WebApplicationFactory<Program> Servidor() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:ImpactoTextil"] = datos.Conexion, ["Catalogo:CarpetaImagenes"] = carpeta }));
    });

    private async Task<HttpClient> Cliente(WebApplicationFactory<Program> servidor, string rol)
    {
        var correo = Guid.NewGuid().ToString("N") + "@example.test";
        await using (var contexto = datos.CrearContexto())
        {
            contexto.Usuario.Add(new Usuario { nombre = "Persona ficticia", correo = correo, passwordHash = new ProtectorContrasena().CrearHash(Clave),
                rol = rol, activo = true, aceptacionTerminos = true, fechaAceptacion = DateTimeOffset.UtcNow });
            await contexto.SaveChangesAsync();
        }
        var cliente = servidor.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        await Token(cliente);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/sesion", new { correo, contrasena = Clave })).StatusCode);
        await Token(cliente);
        return cliente;
    }

    private static async Task Token(HttpClient cliente)
    {
        var cuerpo = await cliente.GetFromJsonAsync<JsonElement>("/api/seguridad/token");
        cliente.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); cliente.DefaultRequestHeaders.Add("X-CSRF-TOKEN", cuerpo.GetProperty("token").GetString());
    }

    private static MultipartFormDataContent Formulario(byte[] contenido, string nombre)
    {
        var formulario = new MultipartFormDataContent(); formulario.Add(new ByteArrayContent(contenido), "archivo", nombre); return formulario;
    }

    private static async Task<string> SubirImagen(HttpClient admin)
    {
        await Token(admin); using var formulario = Formulario(Png, "foto.png");
        var respuesta = await admin.PostAsync("/api/imagenes-catalogo", formulario);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imagenURL").GetString()!;
    }

    private static async Task<ProductoDto> CrearProducto(HttpClient admin, string imagen)
    {
        await Token(admin); var respuesta = await admin.PostAsJsonAsync("/api/administracion/productos", Datos(imagen));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<ProductoDto>())!;
    }

    private static Dictionary<string, object> Datos(string imagen, string? version = null, bool activo = true, string precio = "150.25")
    {
        var datos = new Dictionary<string, object> { ["nombre"] = "Camiseta ficticia", ["color"] = "Blanco", ["talla"] = "M", ["tela"] = "Poliéster",
            ["imagenURL"] = imagen, ["precioBase"] = precio, ["activo"] = activo };
        if (version is not null) datos["version"] = version;
        return datos;
    }

    public void Dispose()
    {
        var ruta = Path.GetFullPath(carpeta);
        if (Path.GetDirectoryName(ruta) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
            || !Path.GetFileName(ruta).StartsWith("ImpactoTextil_CatalogoPruebas_", StringComparison.Ordinal))
            throw new InvalidOperationException("Carpeta de pruebas inesperada.");
        if (!Directory.Exists(ruta)) return;
        foreach (var archivo in Directory.GetFiles(ruta)) File.Delete(archivo);
        Directory.Delete(ruta);
    }
}
