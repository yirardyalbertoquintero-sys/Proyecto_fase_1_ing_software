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

namespace ImpactoTextil.Api.Tests;

public sealed class AdministracionSqlTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>, IDisposable
{
    private const string Clave = "Contraseña ficticia para pruebas";
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "ImpactoTextil_AdminPruebas_" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset FechaCobro = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Png = CatalogoSqlTests.Png;
    private sealed record Solicitud(int Pedido, int Detalle, int Diseno);

    [PruebaSql]
    public async Task Permisos_Csrf_DatosDesconocidos_YFiltrosSeValidanEnServidor()
    {
        using var servidor = Servidor();
        using var visitante = servidor.CreateClient(new() { BaseAddress = new("https://localhost"), AllowAutoRedirect = false });
        using var cliente = await Actor(servidor, "Cliente");
        using var admin = await Actor(servidor);
        foreach (var ruta in new[] { "/api/administracion/pedidos", "/api/administracion/pedidos/1", "/api/administracion/pedidos/1/detalles/1/diseno-original", "/api/administracion/pedidos/1/pagos/1/comprobante" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await visitante.GetAsync(ruta)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/administracion/pedidos/1/sublimacion", new { })).StatusCode);
        admin.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/pedidos/1/archivo", new { motivoArchivo = "Ficticio" })).StatusCode);
        await Token(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/administracion/pedidos?estado=Cancelado")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/administracion/pedidos/1/archivo", new { motivoArchivo = "Ficticio", administradorId = 99 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/administracion/pedidos/2147483647")).StatusCode);
    }

    [PruebaSql]
    public async Task Cotizaciones_Historial_DecisionesInmutables_YConsultaDesdeOtraSesion()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s);
        await AprobarDiseno(admin, p);
        var v1 = await Cotizar(admin, p, "500.00"); await Aceptar(admin, p, v1);
        var v2 = await Cotizar(admin, p, "700.00");
        var antes = await Expediente(admin, p); Assert.Equal(1000m, antes.pedido.total); Assert.Equal(v1, antes.versionCotizacionVigente);
        await Ok(admin.PostAsJsonAsync(Ruta(p, $"cotizaciones/{v2}/decision"), new { decision = "Rechazada", referenciaDecision = "Rechazo ficticio" }));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, $"cotizaciones/{v2}/decision"), new { decision = "Aceptada", referenciaDecision = "Otro acuerdo", fechaEntregaAcordada = "2026-12-10" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, $"cotizaciones/{v1}/decision"), new { decision = "Aceptada", referenciaDecision = "Constancia alterada", fechaEntregaAcordada = "2026-12-10" })).StatusCode);
        await Aceptar(admin, p, v1);
        using var otro = await Actor(s); var despues = await Expediente(otro, p);
        Assert.Equal(2, despues.versionesCotizacion.Count); Assert.Equal(v1, despues.versionCotizacionVigente); Assert.Equal(1000m, despues.pedido.total);
        Assert.Equal(500m, despues.pedido.anticipoExigible); Assert.Contains(despues.constancias, c => c.referencia == "Acuerdo ficticio");
        await using var c = datos.CrearContexto(); Assert.Equal(2, (await c.DetallePedido.SingleAsync(d => d.id == p.Detalle)).cantidad);
    }

    [PruebaSql]
    public async Task Archivos_OriginalYVersion_SeDescarganExactos_ConPertenenciaYContenidoValidado()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s, archivo: true); var otro = await CrearSolicitud(s);
        Assert.Equal(Png, await admin.GetByteArrayAsync(Ruta(p, $"detalles/{p.Detalle}/diseno-original")));
        using (var form = new MultipartFormDataContent())
        {
            form.Add(new ByteArrayContent(Png), "archivo", "../../imagen.png"); form.Add(new StringContent("Vista previa ficticia"), "descripcion");
            var r = await Ok(admin.PostAsync(Ruta(p, $"detalles/{p.Detalle}/disenos/archivo"), form));
            var v = (await r.Content.ReadFromJsonAsync<ResultadoVersionDisenoAdminDto>())!;
            Assert.Equal(Png, await admin.GetByteArrayAsync(Ruta(p, $"detalles/{p.Detalle}/disenos/{v.versionDisenoId}/archivo")));
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Ruta(otro, $"detalles/{p.Detalle}/disenos/{v.versionDisenoId}/archivo"))).StatusCode);
        }
        var archivos = Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories).Length;
        using (var falso = new MultipartFormDataContent())
        { falso.Add(new ByteArrayContent("archivo falso"u8.ToArray()), "archivo", "imagen.png"); Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(Ruta(p, $"detalles/{p.Detalle}/disenos/archivo"), falso)).StatusCode); }
        using (var ajeno = new MultipartFormDataContent())
        { ajeno.Add(new ByteArrayContent(Png), "archivo", "foto.png"); Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync(Ruta(otro, $"detalles/{p.Detalle}/disenos/archivo"), ajeno)).StatusCode); }
        Assert.Equal(archivos, Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories).Length);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/storage/privado/disenos/secreto.png")).StatusCode);
    }

    [PruebaSql]
    public async Task ArchivoAdministrativo_ConservaPedidoEHistorial_YBloqueaOperacionesPosteriores()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s); await Cotizar(admin, p, "500");
        await Ok(admin.PostAsJsonAsync(Ruta(p, "archivo"), new { motivoArchivo = "Desistimiento ficticio" }));
        var x = await Expediente(admin, p); Assert.Equal("Archivada", x.pedido.condicionAdministrativa); Assert.Equal("Pendiente", x.pedido.estado); Assert.Single(x.versionesCotizacion);
        Assert.DoesNotContain((await admin.GetFromJsonAsync<ResumenPedidoAdminDto[]>("/api/administracion/pedidos"))!, v => v.id == p.Pedido);
        Assert.Contains((await admin.GetFromJsonAsync<ResumenPedidoAdminDto[]>("/api/administracion/pedidos?condicionAdministrativa=Archivada"))!, v => v.id == p.Pedido);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, $"detalles/{p.Detalle}/disenos/texto"), new { descripcion = "Nuevo" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "En producción" })).StatusCode);
        var aceptado = await CrearSolicitud(s); await Aceptar(admin, aceptado, await Cotizar(admin, aceptado, "500"));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(aceptado, "archivo"), new { motivoArchivo = "No permitido" })).StatusCode);
    }

    [PruebaSql]
    public async Task EvidenciaPendiente_SeResuelveElMismoPago_SinDoblesCreditosNiReescrituras()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s); await Aceptar(admin, p, await Cotizar(admin, p, "500"));
        var pago = await CrearEvidencia(s, p, 500m); Assert.Equal(1000m, (await Expediente(admin, p)).pedido.saldoPendiente);
        Assert.Equal(Png, await admin.GetByteArrayAsync(Ruta(p, $"pagos/{pago}/comprobante")));
        var decision = new { decision = "Verificado", referencia = "Transferencia ficticia" };
        await Ok(admin.PostAsJsonAsync(Ruta(p, $"pagos/{pago}/decision"), decision)); await Ok(admin.PostAsJsonAsync(Ruta(p, $"pagos/{pago}/decision"), decision));
        var x = await Expediente(admin, p); Assert.Single(x.pagos); Assert.Equal(500m, x.pedido.saldoPendiente); Assert.Equal("Pendiente", x.pedido.estado);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, $"pagos/{pago}/decision"), new { decision = "Rechazado" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, $"pagos/{pago}/decision"), new { decision = "Verificado", referencia = "Transferencia ficticia", fechaCobroConfirmada = FechaCobro.AddDays(1) })).StatusCode);
        var rechazado = await CrearEvidencia(s, p, 100m); await Ok(admin.PostAsJsonAsync(Ruta(p, $"pagos/{rechazado}/decision"), new { decision = "Rechazado" }));
        x = await Expediente(admin, p); Assert.Equal("Rechazado", x.pagos.Single(g => g.id == rechazado).estadoVerificacion); Assert.Equal(500m, x.pedido.saldoPendiente);
    }

    [PruebaSql]
    public async Task PagoSinRespaldoOMontoSuperior_SeDeniega_SinModificarSaldo()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s); await Aceptar(admin, p, await Cotizar(admin, p, "500"));
        var pago = await CrearEvidencia(s, p, 1100m);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, $"pagos/{pago}/decision"), new { decision = "Verificado", referencia = "Exceso" })).StatusCode);
        var sinArchivo = await CrearEvidencia(s, p, 100m, false);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync(Ruta(p, $"pagos/{sinArchivo}/decision"), new { decision = "Verificado", referencia = "Sin archivo" })).StatusCode);
        Assert.Equal(1000m, (await Expediente(admin, p)).pedido.saldoPendiente);
    }

    [PruebaSql]
    public async Task CobroDirecto_IdempotenciaExacta_ReferenciasDuplicadas_YFechaDiferente()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s); await Aceptar(admin, p, await Cotizar(admin, p, "500"));
        var id = Guid.NewGuid(); await Cobrar(admin, p, "500", id, "Recibo 1"); await Cobrar(admin, p, "500", id, "Recibo 1");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "cobros"), Cobro("500", id, "Recibo 1", FechaCobro.AddDays(1)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "cobros"), Cobro("100", Guid.NewGuid(), "Recibo 1"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Ruta(p, "cobros"), Cobro("-1", Guid.NewGuid(), "Negativo"))).StatusCode);
        var x = await Expediente(admin, p); Assert.Single(x.pagos); Assert.Equal(500m, x.pedido.saldoPendiente);
    }

    [PruebaSql]
    public async Task DosAdministradores_CobrosConcurrentesNoGeneranSaldoNegativo()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); var p = await CrearSolicitud(s); await Aceptar(a, p, await Cotizar(a, p, "500"));
        var respuestas = await Task.WhenAll(a.PostAsJsonAsync(Ruta(p, "cobros"), Cobro("700", Guid.NewGuid(), "A")), b.PostAsJsonAsync(Ruta(p, "cobros"), Cobro("700", Guid.NewGuid(), "B")));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        var x = await Expediente(a, p); Assert.Single(x.pagos); Assert.Equal(300m, x.pedido.saldoPendiente);
    }

    [PruebaSql]
    public async Task DosAdministradores_MismaConfirmacionConcurrenteCreaUnSoloCobro()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); var p = await CrearSolicitud(s); await Aceptar(a, p, await Cotizar(a, p, "500"));
        var cobro = Cobro("500", Guid.NewGuid(), "Mismo recibo");
        var respuestas = await Task.WhenAll(a.PostAsJsonAsync(Ruta(p, "cobros"), cobro), b.PostAsJsonAsync(Ruta(p, "cobros"), cobro));
        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode)); var x = await Expediente(a, p); Assert.Single(x.pagos); Assert.Equal(500m, x.pedido.saldoPendiente);
    }

    [PruebaSql]
    public async Task DosEvidenciasConcurrentes_SeRevalidaSaldoDentroDeLaTransaccion()
    {
        using var s = Servidor(); using var a = await Actor(s); using var b = await Actor(s); var p = await CrearSolicitud(s); await Aceptar(a, p, await Cotizar(a, p, "500"));
        var pa = await CrearEvidencia(s, p, 600m); var pb = await CrearEvidencia(s, p, 600m);
        var respuestas = await Task.WhenAll(a.PostAsJsonAsync(Ruta(p, $"pagos/{pa}/decision"), new { decision = "Verificado", referencia = "A" }), b.PostAsJsonAsync(Ruta(p, $"pagos/{pb}/decision"), new { decision = "Verificado", referencia = "B" }));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        var x = await Expediente(a, p); Assert.Equal(400m, x.pedido.saldoPendiente); Assert.Single(x.pagos, g => g.estadoVerificacion == "Pendiente");
    }

    [PruebaSql]
    public async Task Produccion_AumentoExige200Adicionales_YEntregaConConstanciaYSaldoCero()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "Listo para entrega" })).StatusCode);
        await AprobarDiseno(admin, p); await Aceptar(admin, p, await Cotizar(admin, p, "500"));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "En producción" })).StatusCode);
        await Cobrar(admin, p, "500", Guid.NewGuid(), "Primer anticipo"); await Estado(admin, p, "En producción");
        await Ok(admin.PostAsJsonAsync(Ruta(p, "sublimacion"), new { }));
        await Aceptar(admin, p, await Cotizar(admin, p, "700"));
        var x = await Expediente(admin, p); Assert.Equal("En producción", x.pedido.estado); Assert.Equal(700m, x.pedido.anticipoExigible); Assert.Equal(500m, x.pagosVerificadosAcumulados);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "sublimacion"), new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "Listo para entrega" })).StatusCode);
        await Cobrar(admin, p, "200", Guid.NewGuid(), "Completar anticipo"); await Ok(admin.PostAsJsonAsync(Ruta(p, "sublimacion"), new { }));
        await Estado(admin, p, "Listo para entrega");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "Entregado", referenciaConfirmacionEntrega = "Recibido" })).StatusCode);
        await Cobrar(admin, p, "700", Guid.NewGuid(), "Saldo final"); await Estado(admin, p, "Entregado");
        x = await Expediente(admin, p); Assert.Equal("Entregado", x.pedido.estado); Assert.NotNull(x.pedido.fechaEntregaReal); Assert.Equal(0m, x.pedido.saldoPendiente); Assert.Single(x.constancias, c => c.tipo == "Entrega");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "Entregado", referenciaConfirmacionEntrega = "Recibido" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "En producción" })).StatusCode);
    }

    [PruebaSql]
    public async Task RevisionDiseno_ConservaVigenteAnterior_YExigeNuevaComprobacion()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s); await AprobarDiseno(admin, p); await Aceptar(admin, p, await Cotizar(admin, p, "500")); await Cobrar(admin, p, "500", Guid.NewGuid(), "Anticipo"); await Estado(admin, p, "En producción"); await Ok(admin.PostAsJsonAsync(Ruta(p, "sublimacion"), new { }));
        var r = await Ok(admin.PostAsJsonAsync(Ruta(p, $"detalles/{p.Detalle}/disenos/texto"), new { descripcion = "Nueva vista previa ficticia" })); var v = (await r.Content.ReadFromJsonAsync<ResultadoVersionDisenoAdminDto>())!;
        Assert.Equal(p.Diseno, (await Expediente(admin, p)).detalles.Single().versionDisenoVigente);
        await Ok(admin.PostAsJsonAsync(Ruta(p, $"detalles/{p.Detalle}/disenos/{v.versionDisenoId}/decision"), new { decision = "Aprobada", referenciaDecision = "Segunda aprobación" }));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "Listo para entrega" })).StatusCode);
        await Ok(admin.PostAsJsonAsync(Ruta(p, "sublimacion"), new { })); await Estado(admin, p, "Listo para entrega");
        var x = await Expediente(admin, p); Assert.Equal(2, x.versionesDiseno.Count); Assert.All(x.versionesDiseno, d => Assert.Equal("Aprobada", d.decision));
    }

    [PruebaSql]
    public async Task Anticipo_RedondeaArriba_YConstanciaSinPersonalizacionNoExigeImagen()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s, cantidad: 1);
        await AprobarDiseno(admin, p); await Aceptar(admin, p, await Cotizar(admin, p, "100.01")); Assert.Equal(50.01m, (await Expediente(admin, p)).pedido.anticipoExigible);
        await Cobrar(admin, p, "50.00", Guid.NewGuid(), "Primer abono"); Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = "En producción" })).StatusCode);
        await Cobrar(admin, p, "0.01", Guid.NewGuid(), "Centavo"); await Estado(admin, p, "En producción");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.GetAsync(Ruta(p, $"detalles/{p.Detalle}/diseno-original"))).StatusCode);
    }

    [PruebaSql]
    public async Task Repositorio_FalloTrasGuardarPasoRevierteTodosLosCambios()
    {
        using var s = Servidor(); using var admin = await Actor(s); var p = await CrearSolicitud(s);
        await using (var c = datos.CrearContexto())
        {
            var repo = new PedidoAdministracionRepository(c);
            var r = await repo.EjecutarEnPedidoBloqueadoAsync<int>(p.Pedido, async (pedido, guardar, ct) =>
            {
                pedido.fechaEntregaAcordada = new DateOnly(2026, 12, 10); await guardar(ct);
                return ResultadoOperacionAdmin<int>.Fallo(CodigoErrorAdmin.ConflictoEstado, "Fallo simulado después de guardar un paso");
            }, CancellationToken.None); Assert.False(r.EsExito);
        }
        Assert.Null((await Expediente(admin, p)).pedido.fechaEntregaAcordada);
    }

    private WebApplicationFactory<Program> Servidor() => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    { b.UseEnvironment("Testing"); b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:ImpactoTextil"] = datos.Conexion, ["ArchivosPrivados:Carpeta"] = carpeta })); });
    private async Task<HttpClient> Actor(WebApplicationFactory<Program> s, string rol = "Administrador")
    {
        var correo = Guid.NewGuid().ToString("N") + "@example.test";
        await using (var c = datos.CrearContexto()) { c.Usuario.Add(new Usuario { nombre = "Persona ficticia", correo = correo, passwordHash = new ProtectorContrasena().CrearHash(Clave), rol = rol, activo = true, aceptacionTerminos = true, fechaAceptacion = DateTimeOffset.UtcNow }); await c.SaveChangesAsync(); }
        var h = s.CreateClient(new() { BaseAddress = new("https://localhost"), AllowAutoRedirect = false }); await Token(h); await Ok(h.PostAsJsonAsync("/api/sesion", new { correo, contrasena = Clave })); await Token(h); return h;
    }
    private async Task<Solicitud> CrearSolicitud(WebApplicationFactory<Program> s, bool archivo = false, int cantidad = 2)
    {
        string? referencia = null; if (archivo) { using var scope = s.Services.CreateScope(); await using var entrada = new MemoryStream(Png); referencia = await scope.ServiceProvider.GetRequiredService<IAlmacenArchivosPrivados>().GuardarAsync(entrada, TipoArchivoPrivado.DisenoCliente, CancellationToken.None); }
        await using var c = datos.CrearContexto(); var cliente = await c.Usuario.FirstOrDefaultAsync(u => u.rol == "Cliente");
        if (cliente is null) { cliente = new Usuario { nombre = "Cliente ficticio", correo = Guid.NewGuid().ToString("N") + "@example.test", passwordHash = new ProtectorContrasena().CrearHash(Clave), rol = "Cliente", activo = true, aceptacionTerminos = true, fechaAceptacion = DateTimeOffset.UtcNow }; c.Add(cliente); await c.SaveChangesAsync(); }
        var producto = new Producto { nombre = "Prenda ficticia", talla = "M", color = "Blanco", tela = "Poliéster", imagenURL = "/foto-ficticia.png", precioBase = 150m, activo = true }; c.Add(producto); await c.SaveChangesAsync();
        var p = new Pedido { codigo = "T-" + Guid.NewGuid().ToString("N"), cliente = cliente.id, fechaOrden = DateTimeOffset.UtcNow, estado = "Pendiente", condicionAdministrativa = "Activa", modalidadEntrega = "Retiro", confirmacion = Guid.NewGuid(), huellaSolicitud = new byte[32] }; c.Add(p); await c.SaveChangesAsync();
        var d = new DetallePedido { pedido = p.id, producto = producto.id, cantidad = cantidad, talla = "M", color = "Blanco", tela = "Poliéster", empaque = "Sin empaque", ideaDiseno = "Sin personalización", archivoDisenoURL = referencia, precioBaseReferencia = 150m }; c.Add(d); await c.SaveChangesAsync();
        var v = new VersionDiseno { detalle = d.id, numeroVersion = 1, descripcion = "Constancia ficticia Sin personalización", archivoURL = referencia, fechaRegistro = DateTimeOffset.UtcNow, responsableRegistro = cliente.id, decision = "Pendiente" }; c.Add(v); await c.SaveChangesAsync(); return new(p.id, d.id, v.id);
    }
    private async Task<int> CrearEvidencia(WebApplicationFactory<Program> s, Solicitud p, decimal monto, bool archivo = true)
    {
        string? referencia = "comprobantes/" + Guid.NewGuid().ToString("N") + ".png"; if (archivo) { using var scope = s.Services.CreateScope(); await using var entrada = new MemoryStream(Png); referencia = await scope.ServiceProvider.GetRequiredService<IAlmacenArchivosPrivados>().GuardarAsync(entrada, TipoArchivoPrivado.ComprobantePago, CancellationToken.None); }
        await using var c = datos.CrearContexto(); var g = new Pago { pedido = p.Pedido, monto = monto, fecha = FechaCobro, medio = "Transferencia", referencia = "Evidencia " + Guid.NewGuid().ToString("N"), estadoVerificacion = "Pendiente", comprobanteURL = referencia, confirmacion = Guid.NewGuid() }; c.Add(g); await c.SaveChangesAsync(); return g.id;
    }
    private static string Ruta(Solicitud p, string resto = "") => $"/api/administracion/pedidos/{p.Pedido}" + (resto.Length > 0 ? "/" + resto : "");
    private static async Task Token(HttpClient h) { var j = await h.GetFromJsonAsync<JsonElement>("/api/seguridad/token"); h.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); h.DefaultRequestHeaders.Add("X-CSRF-TOKEN", j.GetProperty("token").GetString()); }
    private static async Task<HttpResponseMessage> Ok(Task<HttpResponseMessage> peticion) { var r = await peticion; Assert.True(r.IsSuccessStatusCode, $"HTTP {(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}"); return r; }
    private static async Task<ExpedientePedidoAdminDto> Expediente(HttpClient h, Solicitud p) => (await h.GetFromJsonAsync<ExpedientePedidoAdminDto>(Ruta(p)))!;
    private static async Task AprobarDiseno(HttpClient h, Solicitud p) => await Ok(h.PostAsJsonAsync(Ruta(p, $"detalles/{p.Detalle}/disenos/{p.Diseno}/decision"), new { decision = "Aprobada", referenciaDecision = "Aprobación ficticia" }));
    private static async Task<int> Cotizar(HttpClient h, Solicitud p, string precio) { var r = await Ok(h.PostAsJsonAsync(Ruta(p, "cotizaciones"), new { lineas = new[] { new { detalle = p.Detalle, precioUnitarioAcordado = precio, versionDiseno = (int?)null } } })); return (await r.Content.ReadFromJsonAsync<ResultadoCotizacionAdminDto>())!.versionCotizacionId; }
    private static async Task Aceptar(HttpClient h, Solicitud p, int v) => await Ok(h.PostAsJsonAsync(Ruta(p, $"cotizaciones/{v}/decision"), new { decision = "Aceptada", referenciaDecision = "Acuerdo ficticio", fechaEntregaAcordada = "2026-12-10" }));
    private static object Cobro(string monto, Guid id, string referencia, DateTimeOffset? fecha = null) => new { monto, medio = "Efectivo", referencia, fecha = fecha ?? FechaCobro, confirmacion = id };
    private static async Task Cobrar(HttpClient h, Solicitud p, string monto, Guid id, string referencia) => await Ok(h.PostAsJsonAsync(Ruta(p, "cobros"), Cobro(monto, id, referencia)));
    private static async Task Estado(HttpClient h, Solicitud p, string estado) => await Ok(h.PostAsJsonAsync(Ruta(p, "produccion"), new { nuevoEstado = estado, referenciaConfirmacionEntrega = estado == "Entregado" ? "Recepción ficticia confirmada" : null }));
    public void Dispose()
    {
        var ruta = Path.GetFullPath(carpeta); if (Path.GetDirectoryName(ruta) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(ruta).StartsWith("ImpactoTextil_AdminPruebas_", StringComparison.Ordinal)) throw new InvalidOperationException("Ruta de prueba inesperada.");
        if (Directory.Exists(ruta)) Directory.Delete(ruta, recursive: true);
    }
}
