using System.Net;
using System.Net.Http.Json;
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
using PdfSharp.Pdf;

namespace ImpactoTextil.Api.Tests;

public sealed class PedidosClienteSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>, IDisposable
{
    private const string Clave = "Cliente8!ficticio";
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "ImpactoTextil_Cliente_" + Guid.NewGuid().ToString("N"));
    private static readonly byte[] Png = CatalogoSqlTests.Png;
    private static readonly DateTimeOffset Fecha = new(2026, 10, 31, 12, 30, 0, TimeSpan.Zero);
    private sealed record Caso(int Pedido, int Detalle);
    private WebApplicationFactory<Program> Servidor() => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    { b.UseEnvironment("Testing"); b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:ImpactoTextil"] = datos.Conexion, ["ArchivosPrivados:Carpeta"] = carpeta })); });

    [PruebaSql]
    public async Task Permisos_Csrf_YTitularidadEnListaDetalleCargaYDescarga()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); using var admin = await Actor(s, "Administrador");
        using var anon = s.CreateClient(new() { BaseAddress = new("https://localhost") }); var p = await Crear(a, admin);
        foreach (var url in new[] { "/api/mis-pedidos", Ruta(p), Ruta(p) + "/pagos/1/comprobante" })
        { Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(url)).StatusCode); Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync(url)).StatusCode); }
        Assert.DoesNotContain((await b.GetFromJsonAsync<ResumenPedidoClienteDto[]>("/api/mis-pedidos"))!, x => x.id == p.Pedido);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync(Ruta(p))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(b, p, await Dto(b))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync(Ruta(p) + "/pagos/1/comprobante")).StatusCode);
        a.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(a, p, await Dto(a))).StatusCode);
    }

    [PruebaSql]
    public async Task SinAcuerdo_ImportesNulos_ArchivoVisibleSoloAlTitular_SinRutasNiResponsables()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin, false);
        var x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!;
        Assert.Null(x.pedido.total); Assert.Null(x.pedido.saldoPendiente); Assert.Null(x.pedido.anticipoExigible); Assert.Null(x.pedido.fechaEntregaAcordada);
        Assert.Equal(HttpStatusCode.Conflict, (await Enviar(h, p, await Dto(h))).StatusCode);
        await Ok(admin.PostAsJsonAsync(Admin(p) + "/archivo", new { motivoArchivo = "Desistimiento ficticio" }));
        x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!;
        Assert.Equal("Archivada", x.pedido.condicionAdministrativa); Assert.Single(x.detalles); Assert.Single(x.versionesDiseno);
        Assert.Contains((await h.GetFromJsonAsync<ResumenPedidoClienteDto[]>("/api/mis-pedidos"))!, v => v.id == p.Pedido);
        Assert.Equal(HttpStatusCode.Conflict, (await Enviar(h, p, await Dto(h))).StatusCode);
        var texto = await h.GetStringAsync(Ruta(p)); foreach (var secreto in new[] { "passwordHash", "comprobanteURL", "archivoURL", "responsable", "controlConcurrencia", "huellaComprobante", "clienteCorreo" }) Assert.DoesNotContain(secreto, texto);
    }

    [PruebaSql]
    public async Task EvidenciaCreaPendiente_VerificacionResuelveMismoPago_YAmbosVenSaldoConsistente()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin);
        var r = await Enviar(h, p, await Dto(h)); Assert.Equal(HttpStatusCode.Created, r.StatusCode); var pago = (await r.Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        var x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!;
        Assert.Single(x.pagos); Assert.Equal("Pendiente", x.pagos[0].estadoVerificacion); Assert.Null(x.pagos[0].fechaVerificacion); Assert.Equal(1000m, x.pedido.saldoPendiente);
        Assert.Equal(Png, await h.GetByteArrayAsync(Ruta(p) + $"/pagos/{pago.pagoId}/comprobante"));
        Assert.Equal(Png, await admin.GetByteArrayAsync(Admin(p) + $"/pagos/{pago.pagoId}/comprobante"));
        await Ok(admin.PostAsJsonAsync(Admin(p) + $"/pagos/{pago.pagoId}/decision", new { decision = "Verificado", referencia = "Banco confirmado" }));
        x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!;
        Assert.Single(x.pagos); Assert.Equal(pago.pagoId, x.pagos[0].id); Assert.Equal("Verificado", x.pagos[0].estadoVerificacion); Assert.Equal(500m, x.pedido.saldoPendiente); Assert.Equal("Pendiente", x.pedido.estado);
        var y = (await admin.GetFromJsonAsync<ExpedientePedidoAdminDto>(Admin(p)))!; Assert.Equal(y.pedido.saldoPendiente, x.pedido.saldoPendiente); Assert.Equal(y.pedido.estado, x.pedido.estado);
    }

    [PruebaSql]
    public async Task ReintentoTrasCorreccionAdministrativa_ConservaHuellaOriginalYNoDuplica()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin); var dto = await Dto(h);
        var uno = (await (await Enviar(h, p, dto)).Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        await Ok(admin.PostAsJsonAsync(Admin(p) + $"/pagos/{uno.pagoId}/decision", new { decision = "Verificado", referencia = "Referencia corregida", fechaCobroConfirmada = Fecha.AddDays(-1) }));
        var r = await Enviar(h, p, dto); Assert.Equal(HttpStatusCode.OK, r.StatusCode); var dos = (await r.Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        Assert.Equal(uno.pagoId, dos.pagoId); Assert.False(dos.esCreacionNueva); Assert.Equal("Verificado", dos.estadoVerificacion); Assert.Equal(1, Archivos());
        var diferente = await Dto(h, confirmacion: dto.confirmacion, referencia: "Referencia cambiada"); Assert.Equal(HttpStatusCode.Conflict, (await Enviar(h, p, diferente)).StatusCode);
        await using var c = datos.CrearContexto(); var g = await c.Pago.SingleAsync(g => g.id == uno.pagoId); Assert.Equal(32, g.huellaComprobante!.Length); Assert.Equal(Fecha.AddDays(-1), g.fecha);
    }

    [PruebaSql]
    public async Task RechazoNoAcredita_YNuevaEvidenciaNoSobrescribeRechazo()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin); var d = await Dto(h);
        var g = (await (await Enviar(h, p, d)).Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        await Ok(admin.PostAsJsonAsync(Admin(p) + $"/pagos/{g.pagoId}/decision", new { decision = "Rechazado", referencia = "No recibido" }));
        Assert.Equal(HttpStatusCode.OK, (await Enviar(h, p, d)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Enviar(h, p, await Dto(h, referencia: "Nueva evidencia"))).StatusCode);
        var x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!; Assert.Equal(2, x.pagos.Count); Assert.Equal("Rechazado", x.pagos.Single(v => v.id == g.pagoId).estadoVerificacion); Assert.Equal(1000m, x.pedido.saldoPendiente);
    }

    [PruebaSql]
    public async Task ReenviosConcurrentes_CreanUnPagoUnArchivo_YDuplicadoConOtraClaveSeDeniega()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin); var d = await Dto(h);
        var resultados = await Task.WhenAll(Enviar(h, p, d), Enviar(h, p, d)); Assert.Contains(resultados, r => r.StatusCode == HttpStatusCode.Created); Assert.Contains(resultados, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single((await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!.pagos); Assert.Equal(1, Archivos());
        Assert.Equal(HttpStatusCode.Conflict, (await Enviar(h, p, await Dto(h))).StatusCode);
    }

    [PruebaSql]
    public async Task MismaConfirmacionDistintosClientesNoCompartePagosNiArchivos()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); using var admin = await Actor(s, "Administrador"); var pa = await Crear(a, admin); var pb = await Crear(b, admin); var id = Guid.NewGuid();
        var ga = (await (await Enviar(a, pa, await Dto(a, confirmacion: id))).Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        var gb = (await (await Enviar(b, pb, await Dto(b, confirmacion: id))).Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        Assert.NotEqual(ga.pagoId, gb.pagoId); Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync(Ruta(pa) + $"/pagos/{ga.pagoId}/comprobante")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync(Ruta(pa) + $"/pagos/{gb.pagoId}/comprobante")).StatusCode);
    }

    [PruebaSql]
    public async Task FormularioDeOtraCuenta_DatosDesconocidos_YDatosInvalidosNoPersisten()
    {
        using var s = Servidor(); using var h = await Actor(s); using var b = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin); var d = await Dto(h);
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(b, p, d)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, p, new { d.confirmacion, d.clienteConfirmado, d.monto, d.fecha, d.medio, d.referencia, estadoVerificacion = "Verificado" })).StatusCode);
        foreach (var monto in new[] { 0m, -1m, 1.001m, 10000000000000000m }) Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, p, await Dto(h, monto))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, p, await Dto(h, referencia: "\n"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, p, await Dto(h, medio: "Otro"))).StatusCode);
        Assert.Empty((await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!.pagos); Assert.Equal(0, Archivos());
    }

    [PruebaSql]
    public async Task JpgPngPdf_ContenidoCompleto_YExtensionesConcordantes()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin);
        using var pdf = new PdfDocument(); pdf.AddPage(); using var memoria = new MemoryStream(); pdf.Save(memoria, false); var bytes = memoria.ToArray();
        var r = await Enviar(h, p, await Dto(h), bytes, "pago.pdf"); Assert.Equal(HttpStatusCode.Created, r.StatusCode); var g = (await r.Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        var descarga = await h.GetAsync(Ruta(p) + $"/pagos/{g.pagoId}/comprobante"); Assert.Equal(bytes, await descarga.Content.ReadAsByteArrayAsync()); Assert.Equal("application/pdf", descarga.Content.Headers.ContentType!.MediaType); Assert.Contains("attachment", descarga.Content.Headers.ContentDisposition!.ToString()); Assert.True(descarga.Headers.CacheControl!.NoStore); Assert.Contains("nosniff", descarga.Headers.GetValues("X-Content-Type-Options"));
        foreach (var (contenido, nombre) in new[] { (Png, "pago.exe"), (Png, "pago.jpg"), ("%PDF-1.7\n%%EOF"u8.ToArray(), "pago.pdf"), (Png[..24], "pago.png") })
            Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, p, await Dto(h), contenido, nombre)).StatusCode);
        Assert.Equal(1, Archivos());
    }

    [PruebaSql]
    public async Task CuatroMediosExternos_NoProcesanDineroNiExigenAnticipoPorEvidencia()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin);
        foreach (var medio in ReglasAdministrativasPedido.MediosPagoValidos) Assert.Equal(HttpStatusCode.Created, (await Enviar(h, p, await Dto(h, 1m, medio: medio))).StatusCode);
        var x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!; Assert.Equal(4, x.pagos.Count); Assert.All(x.pagos, g => Assert.Equal("Pendiente", g.estadoVerificacion)); Assert.Equal(1000m, x.pedido.saldoPendiente);
    }

    [PruebaSql]
    public async Task CambiosAdministrativosDeCotizacionYProduccion_SeReflejanEnCliente()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin);
        var e = (await admin.GetFromJsonAsync<ExpedientePedidoAdminDto>(Admin(p)))!; var diseno = e.versionesDiseno.Single();
        await Ok(admin.PostAsJsonAsync(Admin(p) + $"/detalles/{p.Detalle}/disenos/{diseno.id}/decision", new { decision = "Aprobada", referenciaDecision = "Aprobación ficticia" }));
        var g = (await (await Enviar(h, p, await Dto(h))).Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        await Ok(admin.PostAsJsonAsync(Admin(p) + $"/pagos/{g.pagoId}/decision", new { decision = "Verificado", referencia = "Banco ficticio" }));
        await Ok(admin.PostAsJsonAsync(Admin(p) + "/produccion", new { nuevoEstado = "En producción" }));
        var x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!; Assert.Equal("En producción", x.pedido.estado); Assert.Equal(500m, x.pedido.saldoPendiente); Assert.Equal(new DateOnly(2026, 12, 10), x.pedido.fechaEntregaAcordada);
        await CotizarAceptar(admin, p, "700.00"); x = (await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!; Assert.Equal(2, x.versionesCotizacion.Count); Assert.Equal(1400m, x.pedido.total); Assert.Equal(700m, x.pedido.anticipoExigible); Assert.Equal(900m, x.pedido.saldoPendiente); Assert.Equal("En producción", x.pedido.estado);
    }

    [PruebaSql]
    public async Task HuellaAusenteEnCobroHistorico_NoSeConfundeConReintentoCliente()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin); var d = await Dto(h);
        await Ok(admin.PostAsJsonAsync(Admin(p) + "/cobros", new { confirmacion = d.confirmacion, monto = "100.00", fecha = Fecha, medio = "Efectivo", referencia = "Constancia" }));
        Assert.Equal(HttpStatusCode.Conflict, (await Enviar(h, p, d)).StatusCode); Assert.Equal(0, Archivos());
    }

    [PruebaSql]
    public async Task LimiteInclusivoDiezMb_YUnByteAdicionalNoPersiste()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin);
        Assert.Equal(HttpStatusCode.Created, (await Enviar(h, p, await Dto(h), SolicitudesClienteSqlTests.PngConTamano(10_000_000))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(h, p, await Dto(h), SolicitudesClienteSqlTests.PngConTamano(10_000_001))).StatusCode);
        Assert.Single((await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!.pagos); Assert.Equal(1, Archivos());
    }

    [PruebaSql]
    public async Task FalloSqlTrasInsertar_ReviertePagoYLimpiaArchivoNuevo()
    {
        using var s = Servidor(); using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin);
        await using var c = datos.CrearContexto();
        await c.Database.ExecuteSqlRawAsync("CREATE TRIGGER dbo.TR_QA_FalloPago ON dbo.Pago AFTER INSERT AS BEGIN THROW 51010,'Fallo deliberado QA',1; END");
        try
        {
            var r = await Enviar(h, p, await Dto(h)); Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode); Assert.DoesNotContain("Fallo deliberado", await r.Content.ReadAsStringAsync());
            Assert.False(await c.Pago.AnyAsync(g => g.pedido == p.Pedido)); Assert.Equal(0, Archivos());
        }
        finally { await c.Database.ExecuteSqlRawAsync("DROP TRIGGER dbo.TR_QA_FalloPago"); }
    }

    [PruebaSql]
    public async Task CabeceraDeCuentaDesactualizada_NoDevuelveListaNiExpediente()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(b, admin);
        b.DefaultRequestHeaders.Add("X-Cliente-Confirmado", (await Id(a)).ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await b.GetAsync("/api/mis-pedidos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.GetAsync(Ruta(p))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.GetAsync(Ruta(p) + "/pagos/1/comprobante")).StatusCode);
    }

    [PruebaSql]
    public async Task ErrorTrasCommit_NoBorraComprobanteConfirmado_YReintentoRecuperaMismoPago()
    {
        using var s = Servidor().WithWebHostBuilder(b => b.ConfigureServices(servicios => servicios.AddScoped<IPedidoAdministracionRepository>(sp => new FalloTrasCommit(new PedidoAdministracionRepository(sp.GetRequiredService<ImpactoTextilDbContext>())))));
        using var h = await Actor(s); using var admin = await Actor(s, "Administrador"); var p = await Crear(h, admin); var d = await Dto(h);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Enviar(h, p, d)).StatusCode); Assert.Equal(1, Archivos());
        var r = await Enviar(h, p, d); Assert.Equal(HttpStatusCode.OK, r.StatusCode); var g = (await r.Content.ReadFromJsonAsync<ComprobanteRecibidoDto>())!;
        Assert.Equal(Png, await h.GetByteArrayAsync(Ruta(p) + $"/pagos/{g.pagoId}/comprobante"));
        Assert.Single((await h.GetFromJsonAsync<ExpedientePedidoClienteDto>(Ruta(p)))!.pagos);
    }

    private sealed class FalloTrasCommit(IPedidoAdministracionRepository inner) : IPedidoAdministracionRepository
    {
        public async Task<ResultadoOperacionAdmin<T>> EjecutarEnPedidoBloqueadoAsync<T>(int p,
            Func<Pedido, Func<CancellationToken, Task>, CancellationToken, Task<ResultadoOperacionAdmin<T>>> op, CancellationToken ct)
        {
            var r = await inner.EjecutarEnPedidoBloqueadoAsync(p, op, ct);
            if (r.EsExito && r.Valor is ComprobanteRecibidoDto { esCreacionNueva: true }) throw new IOException("Fallo QA después del commit");
            return r;
        }
        public Task<Usuario?> ObtenerAdministradorActivoAsync(int a, CancellationToken ct) => inner.ObtenerAdministradorActivoAsync(a, ct);
        public Task<IReadOnlyList<ResumenPedidoAdminDto>> ListarPedidosAsync(string? e, string c, CancellationToken ct) => inner.ListarPedidosAsync(e, c, ct);
        public Task<ExpedientePedidoAdminDto?> ObtenerExpedienteAsync(int p, CancellationToken ct) => inner.ObtenerExpedienteAsync(p, ct);
        public Task<ReferenciaArchivoDiseno> ObtenerReferenciaComprobanteAsync(int p, int g, CancellationToken ct) => inner.ObtenerReferenciaComprobanteAsync(p, g, ct);
        public Task<bool> ArchivoDisenoReferenciadoAsync(string r, CancellationToken ct) => inner.ArchivoDisenoReferenciadoAsync(r, ct);
        public Task<ReferenciaArchivoDiseno> ObtenerReferenciaDisenoOriginalAsync(int p, int d, CancellationToken ct) => inner.ObtenerReferenciaDisenoOriginalAsync(p, d, ct);
        public Task<ReferenciaArchivoDiseno> ObtenerReferenciaVersionDisenoAsync(int p, int d, int v, CancellationToken ct) => inner.ObtenerReferenciaVersionDisenoAsync(p, d, v, ct);
    }

    private async Task<HttpClient> Actor(WebApplicationFactory<Program> s, string rol = "Cliente")
    {
        var correo = Guid.NewGuid().ToString("N") + "@example.invalid";
        await using (var c = datos.CrearContexto()) { c.Add(new Usuario { nombre = "Persona ficticia", correo = correo, passwordHash = new ProtectorContrasena().CrearHash(Clave), rol = rol, activo = true, aceptacionTerminos = rol == "Cliente", fechaAceptacion = rol == "Cliente" ? DateTimeOffset.UtcNow : null }); await c.SaveChangesAsync(); }
        var h = s.CreateClient(new() { BaseAddress = new("https://localhost"), AllowAutoRedirect = false }); await Token(h); await Ok(h.PostAsJsonAsync("/api/sesion", new { correo, contrasena = Clave })); await Token(h); return h;
    }
    private static async Task<int> Id(HttpClient h) => (await h.GetFromJsonAsync<JsonElement>("/api/sesion")).GetProperty("id").GetInt32();
    private async Task<Caso> Crear(HttpClient h, HttpClient admin, bool cotizado = true)
    {
        int producto; await using (var c = datos.CrearContexto()) { var p = new Producto { nombre = "Prenda ficticia", talla = "M", color = "Blanco", tela = "Poliéster", imagenURL = "/foto-ficticia.png", activo = true, precioBase = 150m }; c.Add(p); await c.SaveChangesAsync(); producto = p.id; }
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(JsonSerializer.Serialize(new { confirmacion = Guid.NewGuid(), clienteConfirmado = await Id(h), modalidadEntrega = "Retiro", totalReferencialConfirmado = "300.00", detalles = new[] { new { producto, cantidad = 2, talla = "M", color = "Blanco", tela = "Poliéster", empaque = "Bolsa estándar", sinPersonalizacion = true, precioBaseConfirmado = "150.00" } } })), "datos");
        var r = await Ok(h.PostAsync("/api/solicitudes", form)); var creado = (await r.Content.ReadFromJsonAsync<SolicitudCreadaDto>())!;
        var e = (await admin.GetFromJsonAsync<ExpedientePedidoAdminDto>("/api/administracion/pedidos/" + creado.id))!; var caso = new Caso(creado.id, e.detalles[0].id);
        if (cotizado) await CotizarAceptar(admin, caso, "500.00"); return caso;
    }
    private static async Task CotizarAceptar(HttpClient admin, Caso p, string precio)
    {
        var r = await Ok(admin.PostAsJsonAsync(Admin(p) + "/cotizaciones", new { lineas = new[] { new { detalle = p.Detalle, precioUnitarioAcordado = precio } } }));
        var v = (await r.Content.ReadFromJsonAsync<ResultadoCotizacionAdminDto>())!;
        await Ok(admin.PostAsJsonAsync(Admin(p) + $"/cotizaciones/{v.versionCotizacionId}/decision", new { decision = "Aceptada", referenciaDecision = "Acuerdo ficticio", fechaEntregaAcordada = "2026-12-10" }));
    }
    private static async Task<RegistrarComprobanteDto> Dto(HttpClient h, decimal monto = 500m, Guid? confirmacion = null, string referencia = "Declaración ficticia", string medio = "Transferencia") => new() { confirmacion = confirmacion ?? Guid.NewGuid(), clienteConfirmado = await Id(h), monto = monto, medio = medio, referencia = referencia, fecha = Fecha };
    private static async Task<HttpResponseMessage> Enviar(HttpClient h, Caso p, object d, byte[]? archivo = null, string nombre = "comprobante.png") { using var f = new MultipartFormDataContent(); f.Add(new StringContent(JsonSerializer.Serialize(d)), "datos"); f.Add(new ByteArrayContent(archivo ?? Png), "archivo", nombre); return await h.PostAsync(Ruta(p) + "/comprobantes", f); }
    private static string Ruta(Caso p) => "/api/mis-pedidos/" + p.Pedido;
    private static string Admin(Caso p) => "/api/administracion/pedidos/" + p.Pedido;
    private static async Task<HttpResponseMessage> Ok(Task<HttpResponseMessage> tarea) { var r = await tarea; Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync()); return r; }
    private static async Task Token(HttpClient h) { var t = await h.GetFromJsonAsync<JsonElement>("/api/seguridad/token"); h.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); h.DefaultRequestHeaders.Add("X-CSRF-TOKEN", t.GetProperty("token").GetString()); }
    private int Archivos() => Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories).Length : 0;
    public void Dispose() { var p = Path.GetFullPath(carpeta); if (Path.GetDirectoryName(p) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(p).StartsWith("ImpactoTextil_Cliente_", StringComparison.Ordinal)) throw new InvalidOperationException("Carpeta QA inválida"); if (Directory.Exists(p)) Directory.Delete(p, true); }
}
