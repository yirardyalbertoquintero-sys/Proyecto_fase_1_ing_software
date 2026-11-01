using System.Net;
using System.Net.Http.Json;
using System.Buffers.Binary;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImpactoTextil.Api.Tests;

public sealed class SolicitudesClienteSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>, IDisposable
{
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "ImpactoTextil_Solicitudes_" + Guid.NewGuid().ToString("N"));
    private const string Clave = "Cliente8!ficticio";
    private static readonly byte[] Png = CatalogoSqlTests.Png;

    [PruebaSql]
    public async Task Permisos_Csrf_CamposDesconocidos_YAdjuntosAjenosSeRechazan()
    {
        using var s = Servidor(); using var anon = s.CreateClient(new() { BaseAddress = new("https://localhost") }); using var admin = await Actor(s, "Administrador"); using var cliente = await Actor(s);
        var p = await Producto(); var d = Solicitud(p);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(anon, d)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(admin, d)).StatusCode);
        cliente.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(cliente, d)).StatusCode); await Token(cliente);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(cliente, new { d.confirmacion, d.modalidadEntrega, d.direccionEntrega, d.totalReferencialConfirmado, d.detalles, cliente = 99 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(cliente, d, [("ajeno", Png)])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(cliente, Solicitud(p, archivo: true))).StatusCode);
        await using var c = datos.CrearContexto(); Assert.False(await c.Pedido.AnyAsync(x => x.confirmacion == d.confirmacion));
    }

    [PruebaSql]
    public async Task SolicitudVariasPrendas_Pendiente_SinCotizacion_VisibleEnAdministracion_YOriginalExacto()
    {
        using var s = Servidor(); using var cliente = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Producto();
        var d = Solicitud(p, archivo: true); d.detalles!.Add(Linea(p));
        d = Copiar(d, total: 600m);
        var r = await Enviar(cliente, d, [("diseno-0", Png)]); Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var x = (await r.Content.ReadFromJsonAsync<SolicitudCreadaDto>())!; Assert.Equal(600m, x.totalReferencial); Assert.True(x.esCreacionNueva);
        var expediente = (await admin.GetFromJsonAsync<ExpedientePedidoAdminDto>($"/api/administracion/pedidos/{x.id}"))!;
        Assert.Equal("Pendiente", expediente.pedido.estado); Assert.Null(expediente.pedido.total); Assert.Null(expediente.pedido.saldoPendiente);
        Assert.Equal(2, expediente.detalles.Count); Assert.Empty(expediente.versionesCotizacion); Assert.Empty(expediente.pagos);
        Assert.All(expediente.versionesDiseno, v => Assert.Equal("Pendiente", v.decision));
        var conArchivo = expediente.detalles.Single(v => v.tieneArchivoOriginal);
        Assert.Equal(Png, await admin.GetByteArrayAsync($"/api/administracion/pedidos/{x.id}/detalles/{conArchivo.id}/diseno-original"));
        await using var c = datos.CrearContexto(); var guardados = await c.DetallePedido.Where(v => v.pedido == x.id).ToListAsync();
        Assert.All(guardados, v => { Assert.Equal("Bolsa estándar", v.empaque); Assert.Null(v.versionDisenoVigente); });
        Assert.Contains(guardados, v => v.ideaDiseno == "Sin personalización");
        var dueno = await c.Pedido.Where(v => v.id == x.id).Select(v => v.cliente).SingleAsync();
        Assert.All(await c.VersionDiseno.Where(v => guardados.Select(g => g.id).Contains(v.detalle)).ToListAsync(), v => Assert.Equal(dueno, v.responsableRegistro));
    }

    [PruebaSql]
    public async Task CatalogoCambiadoEnPrecioVarianteOActivo_ExigeNuevaConfirmacion_SinGuardarArchivos()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true);
        foreach (var cambio in new[] { "precio", "talla", "activo" })
        {
            await using (var c = datos.CrearContexto()) { var v = await c.Producto.SingleAsync(v => v.id == p.id); v.precioBase = cambio == "precio" ? 151m : 150m; v.talla = cambio == "talla" ? "L" : "M"; v.activo = cambio != "activo"; await c.SaveChangesAsync(); }
            var r = await Enviar(h, d, [("diseno-0", Png)]); Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
            Assert.Equal("catalogo", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campo").GetString());
        }
        await using var fin = datos.CrearContexto(); Assert.False(await fin.Pedido.AnyAsync(v => v.confirmacion == d.confirmacion)); Assert.Equal(0, Archivos());
    }

    [PruebaSql]
    public async Task PreciosPorLineaSeComparanAunqueElTotalNoCambie()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var p2 = await Producto(); var d = Solicitud(p); d.detalles!.Add(Linea(p2)); d = Copiar(d, total: 600m);
        await using (var c = datos.CrearContexto()) { var a = await c.Producto.SingleAsync(x => x.id == p.id); var b = await c.Producto.SingleAsync(x => x.id == p2.id); a.precioBase = 151m; b.precioBase = 149m; await c.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Conflict, (await Enviar(h, d)).StatusCode);
        await using var fin = datos.CrearContexto(); Assert.False(await fin.Pedido.AnyAsync(v => v.confirmacion == d.confirmacion));
    }

    [PruebaSql]
    public async Task ReenvioRecuperaElMismoPedidoAunqueDespuesCambieElCatalogo_YConservaArchivo()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true);
        var uno = await Enviar(h, d, [("diseno-0", Png)]); Assert.Equal(HttpStatusCode.Created, uno.StatusCode); var a = (await uno.Content.ReadFromJsonAsync<SolicitudCreadaDto>())!;
        await using (var c = datos.CrearContexto()) { var v = await c.Producto.SingleAsync(v => v.id == p.id); v.precioBase = 999m; v.activo = false; await c.SaveChangesAsync(); }
        var dos = await Enviar(h, d, [("diseno-0", Png)]); Assert.Equal(HttpStatusCode.OK, dos.StatusCode);
        var textoReintento = await dos.Content.ReadAsStringAsync();
        var b = JsonSerializer.Deserialize<SolicitudCreadaDto>(textoReintento, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(a.id, b.id); Assert.Equal(a.codigo, b.codigo); Assert.False(b.esCreacionNueva); Assert.Equal(300m, b.totalReferencial); Assert.Equal(1, Archivos());
        var jsonReintento = JsonSerializer.Deserialize<JsonElement>(textoReintento);
        Assert.Equal("300.00", jsonReintento.GetProperty("totalReferencial").GetString());
    }

    [PruebaSql]
    public async Task MismaConfirmacionDistintoContenido_NoCreaOtroPedido()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p);
        Assert.Equal(HttpStatusCode.Created, (await Enviar(h, d)).StatusCode);
        var otro = Copiar(d, total: 450m, detalles: [Linea(p, cantidad: 3)]);
        Assert.Equal(HttpStatusCode.Conflict, (await Enviar(h, otro)).StatusCode);
        await using var c = datos.CrearContexto(); Assert.Equal(1, await c.Pedido.CountAsync(v => v.confirmacion == d.confirmacion));
    }

    [PruebaSql]
    public async Task DosReenviosSimultaneos_CreanUnaSolaSolicitudYUnSoloArchivo()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true);
        var rr = await Task.WhenAll(Enviar(h, d, [("diseno-0", Png)]), Enviar(h, d, [("diseno-0", Png)]));
        Assert.Single(rr, v => v.StatusCode == HttpStatusCode.Created); Assert.Single(rr, v => v.StatusCode == HttpStatusCode.OK);
        await using var c = datos.CrearContexto(); Assert.Equal(1, await c.Pedido.CountAsync(v => v.confirmacion == d.confirmacion)); Assert.Equal(1, Archivos());
    }

    [PruebaSql]
    public async Task ClientesDistintos_PuedenUsarLaMismaConfirmacionSinCompartirPedido()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); var p = await Producto(); var d = Solicitud(p);
        var aa = await Enviar(a, d); var bb = await Enviar(b, d); Assert.Equal(HttpStatusCode.Created, aa.StatusCode); Assert.Equal(HttpStatusCode.Created, bb.StatusCode);
        Assert.NotEqual((await aa.Content.ReadFromJsonAsync<SolicitudCreadaDto>())!.id, (await bb.Content.ReadFromJsonAsync<SolicitudCreadaDto>())!.id);
    }

    [PruebaSql]
    public async Task SegundoArchivoInvalido_RevierteTodoYLimpiaSoloSusArchivos()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true); d.detalles!.Add(Linea(p, archivo: "diseno-1")); d = Copiar(d, total: 600m);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, d, [("diseno-0", Png), ("diseno-1", Png[..24])])).StatusCode);
        await using var c = datos.CrearContexto(); Assert.False(await c.Pedido.AnyAsync(v => v.confirmacion == d.confirmacion)); Assert.Equal(0, Archivos());
    }

    [PruebaSql]
    public async Task FalloSqlTrasInsertarPedidoYDetalle_RevierteSolicitudYArchivos()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true, idea: "Fallo simulado CU06");
        await using var c = datos.CrearContexto();
        await c.Database.ExecuteSqlRawAsync("CREATE TRIGGER TR_Prueba_CU06_fallo ON dbo.VersionDiseno AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE descripcion=N'Fallo simulado CU06') THROW 51000, 'Fallo de QA', 1;");
        try
        {
            var r = await Enviar(h, d, [("diseno-0", Png)]); Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
            Assert.DoesNotContain("Fallo de QA", await r.Content.ReadAsStringAsync());
            Assert.False(await c.Pedido.AnyAsync(v => v.confirmacion == d.confirmacion)); Assert.Equal(0, Archivos());
        }
        finally { await c.Database.ExecuteSqlRawAsync("DROP TRIGGER dbo.TR_Prueba_CU06_fallo"); }
    }

    [PruebaSql]
    public async Task CantidadDireccionDisenoYEmpaque_SeValidanAntesDeCrear()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto();
        foreach (var d in new[] { Solicitud(p, cantidad: 0), Solicitud(p, modalidad: "Domicilio"), Solicitud(p, idea: ""), Solicitud(p, empaque: "Caja premium"), Solicitud(p, empaque: "Sin empaque") })
            Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, d)).StatusCode);
        var domicilio = Solicitud(p, modalidad: "Domicilio", direccion: " Calle ficticia 10 ");
        var r = await Enviar(h, domicilio); Assert.Equal(HttpStatusCode.Created, r.StatusCode); var id = (await r.Content.ReadFromJsonAsync<SolicitudCreadaDto>())!.id;
        await using var c = datos.CrearContexto(); Assert.Equal("Calle ficticia 10", (await c.Pedido.SingleAsync(v => v.id == id)).direccionEntrega);
    }

    [PruebaSql]
    public async Task DosImagenesDeDiezMb_SeAceptanEnElLimite_YUnByteAdicionalSeRechaza()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true);
        d.detalles!.Add(Linea(p, archivo: "diseno-1")); d = Copiar(d, total: 600m);
        var grande = PngConTamano(10_000_000);
        Assert.Equal(HttpStatusCode.Created, (await Enviar(h, d, [("diseno-0", grande), ("diseno-1", grande)])).StatusCode);
        var otro = Solicitud(p, archivo: true);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, otro, [("diseno-0", PngConTamano(10_000_001))])).StatusCode);
        Assert.Equal(2, Archivos());
    }

    [PruebaSql]
    public async Task ErrorDespuesDelCommit_ConservaArchivo_YReintentoRecuperaSolicitud()
    {
        var fallar = 1;
        using var s = Servidor().WithWebHostBuilder(b => b.ConfigureServices(services => services.AddScoped<ISolicitudClienteRepository>(sp =>
            new FalloTrasCommit(new SolicitudClienteRepository(sp.GetRequiredService<ImpactoTextilDbContext>()), () => Interlocked.Exchange(ref fallar, 0) == 1))));
        using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Enviar(h, d, [("diseno-0", Png)])).StatusCode);
        Assert.Equal(1, Archivos());
        var r = await Enviar(h, d, [("diseno-0", Png)]); Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        await using var c = datos.CrearContexto(); Assert.Equal(1, await c.Pedido.CountAsync(v => v.confirmacion == d.confirmacion));
        var pedido = await c.Pedido.SingleAsync(v => v.confirmacion == d.confirmacion);
        var referencia = await c.DetallePedido.Where(v => v.pedido == pedido.id).Select(v => v.archivoDisenoURL).SingleAsync();
        Assert.Equal(Png, await File.ReadAllBytesAsync(Path.Combine(carpeta, referencia!)));
    }

    [PruebaSql]
    public async Task FormularioDeOtraCuenta_NoPuedeCrearPedidoBajoLaSesionActual()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); var p = await Producto();
        var sesion = await a.GetFromJsonAsync<JsonElement>("/api/sesion");
        var d = Copiar(Solicitud(p), 300m, cliente: sesion.GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(b, d)).StatusCode);
        await using var c = datos.CrearContexto(); Assert.False(await c.Pedido.AnyAsync(v => v.confirmacion == d.confirmacion));
    }

    [PruebaSql]
    public async Task ExtensionNoAdmitidaOFalsa_SeRechazaAunqueLosBytesSeanPngReal()
    {
        using var s = Servidor(); using var h = await Actor(s); var p = await Producto(); var d = Solicitud(p, archivo: true);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, d, [("diseno-0", Png)], "diseno.exe")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, d, [("diseno-0", Png)], "diseno.jpg")).StatusCode);
        await using var c = datos.CrearContexto(); Assert.False(await c.Pedido.AnyAsync(v => v.confirmacion == d.confirmacion)); Assert.Equal(0, Archivos());
    }

    private sealed class FalloTrasCommit(ISolicitudClienteRepository inner, Func<bool> fallar) : ISolicitudClienteRepository
    {
        public async Task<ResultadoOperacionAdmin<SolicitudCreadaDto>> RegistrarAsync(int id, RegistrarSolicitudDto d, byte[] huella,
            Func<IReadOnlyList<Producto>, CancellationToken, Task<ResultadoOperacionAdmin<Pedido>>> construir, CancellationToken ct)
        { var r = await inner.RegistrarAsync(id, d, huella, construir, ct); if (r.EsExito && r.Valor!.esCreacionNueva && fallar()) throw new IOException("Respuesta de QA perdida tras commit"); return r; }
        public Task<bool> ArchivoReferenciadoAsync(string r, CancellationToken ct) => inner.ArchivoReferenciadoAsync(r, ct);
    }
    private static byte[] PngConTamano(int tamano)
    {
        var relleno = tamano - Png.Length - 12; var b = new byte[tamano]; Png.AsSpan(0, 33).CopyTo(b);
        BinaryPrimitives.WriteInt32BigEndian(b.AsSpan(33, 4), relleno); "caNd"u8.CopyTo(b.AsSpan(37, 4));
        uint crc = uint.MaxValue; foreach (var v in b.AsSpan(37, relleno + 4)) { crc ^= v; for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1; }
        BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(41 + relleno, 4), ~crc); Png.AsSpan(33).CopyTo(b.AsSpan(45 + relleno)); return b;
    }

    private WebApplicationFactory<Program> Servidor() => new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseEnvironment("Testing"); b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:ImpactoTextil"] = datos.Conexion, ["ArchivosPrivados:Carpeta"] = carpeta })); });
    private async Task<HttpClient> Actor(WebApplicationFactory<Program> s, string rol = "Cliente")
    {
        var correo = Guid.NewGuid().ToString("N") + "@example.test"; await using (var c = datos.CrearContexto()) { c.Add(new Usuario { nombre = "Cliente ficticio", correo = correo, passwordHash = new ProtectorContrasena().CrearHash(Clave), rol = rol, activo = true, aceptacionTerminos = rol == "Cliente", fechaAceptacion = rol == "Cliente" ? DateTimeOffset.UtcNow : null }); await c.SaveChangesAsync(); }
        var h = s.CreateClient(new() { BaseAddress = new("https://localhost"), AllowAutoRedirect = false }); await Token(h); Assert.True((await h.PostAsJsonAsync("/api/sesion", new { correo, contrasena = Clave })).IsSuccessStatusCode); await Token(h); return h;
    }
    private async Task<Producto> Producto() { await using var c = datos.CrearContexto(); var p = new Producto { nombre = "Prenda CU06 ficticia", talla = "M", color = "Blanco", tela = "Poliéster", imagenURL = "/ficticia.png", precioBase = 150m, activo = true }; c.Add(p); await c.SaveChangesAsync(); return p; }
    private static ConfiguracionPrendaDto Linea(Producto p, int cantidad = 2, string? archivo = null, string? idea = "Idea ficticia", string empaque = "Bolsa estándar") => new() { producto = p.id, cantidad = cantidad, talla = p.talla, color = p.color, tela = p.tela, empaque = empaque, precioBaseConfirmado = p.precioBase, sinPersonalizacion = archivo is null && idea == "Idea ficticia", ideaDiseno = archivo is null && idea == "Idea ficticia" ? null : idea, archivo = archivo };
    private static RegistrarSolicitudDto Solicitud(Producto p, bool archivo = false, int cantidad = 2, string modalidad = "Retiro", string? direccion = null, string? idea = "Idea ficticia", string empaque = "Bolsa estándar") => new() { confirmacion = Guid.NewGuid(), modalidadEntrega = modalidad, direccionEntrega = direccion, totalReferencialConfirmado = p.precioBase * cantidad, detalles = [Linea(p, cantidad, archivo ? "diseno-0" : null, idea, empaque)] };
    private static RegistrarSolicitudDto Copiar(RegistrarSolicitudDto d, decimal total, List<ConfiguracionPrendaDto?>? detalles = null, int? cliente = null) => new() { confirmacion = d.confirmacion, clienteConfirmado = cliente ?? d.clienteConfirmado, modalidadEntrega = d.modalidadEntrega, direccionEntrega = d.direccionEntrega, totalReferencialConfirmado = total, detalles = detalles ?? d.detalles };
    private static async Task<HttpResponseMessage> Enviar(HttpClient h, object d, (string nombre, byte[] datos)[]? archivos = null, string nombreArchivo = "diseno.png")
    {
        if (d is RegistrarSolicitudDto { clienteConfirmado: 0 } typed) { var session = await h.GetAsync("/api/sesion"); if (session.IsSuccessStatusCode) { var who = await session.Content.ReadFromJsonAsync<JsonElement>(); d = Copiar(typed, typed.totalReferencialConfirmado!.Value, cliente: who.GetProperty("id").GetInt32()); } }
        using var f = new MultipartFormDataContent(); f.Add(new StringContent(JsonSerializer.Serialize(d)), "datos"); foreach (var (nombre, bytes) in archivos ?? []) f.Add(new ByteArrayContent(bytes), nombre, nombreArchivo); return await h.PostAsync("/api/solicitudes", f);
    }
    private static async Task Token(HttpClient h) { var t = await h.GetFromJsonAsync<JsonElement>("/api/seguridad/token"); h.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); h.DefaultRequestHeaders.Add("X-CSRF-TOKEN", t.GetProperty("token").GetString()); }
    private int Archivos() => Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories).Length : 0;
    public void Dispose() { var p = Path.GetFullPath(carpeta); if (Path.GetDirectoryName(p) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(p).StartsWith("ImpactoTextil_Solicitudes_", StringComparison.Ordinal)) throw new InvalidOperationException("Carpeta QA inválida"); if (Directory.Exists(p)) Directory.Delete(p, true); }
}
