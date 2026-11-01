using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Controllers;

[ApiController, Authorize(Policy = "Administrador")]
[Route("api/administracion/pedidos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PedidosAdministracionController(
    RevisionCotizacionAdminService revision,
    DescargaDisenoAdminService disenos,
    DescargaComprobanteAdminService comprobantes,
    PagosAdminService pagos,
    ProduccionEntregaAdminService produccion,
    IAlmacenArchivosPrivados almacen,
    IPedidoAdministracionRepository repositorio,
    ILogger<PedidosAdministracionController> logger) : ControllerBase
{
    private int AdministradorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] FiltroPedidosAdminDto filtro, CancellationToken ct) =>
        Responder(await revision.ListarPedidosAsync(AdministradorId, filtro, ct));

    [HttpGet("{pedidoId:int:min(1)}")]
    public async Task<IActionResult> Expediente(int pedidoId, CancellationToken ct) =>
        Responder(await revision.ObtenerExpedienteAsync(AdministradorId, pedidoId, ct));

    [HttpPost("{pedidoId:int:min(1)}/cotizaciones"), RequestSizeLimit(131_072)]
    public async Task<IActionResult> Proponer(int pedidoId, ProponerCotizacionDto solicitud, CancellationToken ct) =>
        Responder(await revision.ProponerCotizacionAsync(AdministradorId, pedidoId, solicitud, ct));

    [HttpPost("{pedidoId:int:min(1)}/cotizaciones/{versionId:int:min(1)}/decision"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> DecidirCotizacion(int pedidoId, int versionId, DecidirCotizacionDto solicitud, CancellationToken ct) =>
        Responder(await revision.DecidirCotizacionAsync(AdministradorId, pedidoId, versionId, solicitud, ct));

    [HttpPost("{pedidoId:int:min(1)}/detalles/{detalleId:int:min(1)}/disenos/texto"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> RegistrarTexto(int pedidoId, int detalleId, RegistrarVersionDisenoTextoDto solicitud, CancellationToken ct) =>
        Responder(await revision.IncorporarVersionDisenoAsync(AdministradorId, pedidoId, detalleId, null, solicitud.descripcion, ct));

    [HttpPost("{pedidoId:int:min(1)}/detalles/{detalleId:int:min(1)}/disenos/archivo")]
    [RequestSizeLimit(10_050_000), RequestFormLimits(MultipartBodyLengthLimit = 10_050_000)]
    public async Task<IActionResult> RegistrarArchivo(int pedidoId, int detalleId, [FromForm] SubirDisenoAdminDto solicitud, CancellationToken ct)
    {
        if (solicitud.archivo.Length is <= 0 or > ValidadorArchivosPrivados.MaximoBytesInclusivo)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Selecciona una imagen JPG/PNG de hasta 10,000,000 bytes." });
        string? referencia = null;
        try
        {
            await using var entrada = solicitud.archivo.OpenReadStream();
            referencia = await almacen.GuardarAsync(entrada, TipoArchivoPrivado.DisenoCliente, ct);
            var resultado = await revision.IncorporarVersionDisenoAsync(AdministradorId, pedidoId, detalleId, referencia, solicitud.descripcion, ct);
            if (!resultado.EsExito) await LimpiarSiNoReferenciado(referencia);
            return Responder(resultado);
        }
        catch (InvalidDataException error)
        { return BadRequest(new ProblemDetails { Status = 400, Title = error.Message }); }
        catch
        {
            // Una cancelación puede ocurrir después del commit. Nunca eliminar un
            // archivo confirmado; si no se puede comprobar, conservarlo y registrar.
            if (referencia is not null) await LimpiarSiNoReferenciado(referencia);
            throw;
        }
    }

    private async Task LimpiarSiNoReferenciado(string referencia)
    {
        try
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (!await repositorio.ArchivoDisenoReferenciadoAsync(referencia, limite.Token))
                await almacen.EliminarSiExisteAsync(referencia, limite.Token);
        }
        catch (Exception error) { logger.LogWarning(error, "Revisar archivo privado sin confirmación de limpieza: {Referencia}", referencia); }
    }

    [HttpPost("{pedidoId:int:min(1)}/detalles/{detalleId:int:min(1)}/disenos/{versionId:int:min(1)}/decision"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> DecidirDiseno(int pedidoId, int detalleId, int versionId, DecidirVersionDisenoDto solicitud, CancellationToken ct) =>
        Responder(await revision.DecidirVersionDisenoAsync(AdministradorId, pedidoId, detalleId, versionId, solicitud, ct));

    [HttpPost("{pedidoId:int:min(1)}/archivo"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> Archivar(int pedidoId, ArchivarPedidoDto solicitud, CancellationToken ct) =>
        Responder(await revision.ArchivarSolicitudAsync(AdministradorId, pedidoId, solicitud, ct));

    [HttpGet("{pedidoId:int:min(1)}/detalles/{detalleId:int:min(1)}/diseno-original")]
    public async Task<IActionResult> Original(int pedidoId, int detalleId, CancellationToken ct) =>
        Descargar(await disenos.DescargarDisenoOriginalAsync(AdministradorId, pedidoId, detalleId, ct));

    [HttpGet("{pedidoId:int:min(1)}/detalles/{detalleId:int:min(1)}/disenos/{versionId:int:min(1)}/archivo")]
    public async Task<IActionResult> Version(int pedidoId, int detalleId, int versionId, CancellationToken ct) =>
        Descargar(await disenos.DescargarArchivoVersionDisenoAsync(AdministradorId, pedidoId, detalleId, versionId, ct));

    [HttpGet("{pedidoId:int:min(1)}/pagos/{pagoId:int:min(1)}/comprobante")]
    public async Task<IActionResult> Comprobante(int pedidoId, int pagoId, CancellationToken ct) =>
        Descargar(await comprobantes.DescargarAsync(AdministradorId, pedidoId, pagoId, ct));

    [HttpPost("{pedidoId:int:min(1)}/pagos/{pagoId:int:min(1)}/decision"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> ResolverPago(int pedidoId, int pagoId, ResolverPagoPendienteDto solicitud, CancellationToken ct)
    {
        if (solicitud.decision?.Trim() == "Verificado")
        {
            var evidencia = await comprobantes.DescargarAsync(AdministradorId, pedidoId, pagoId, ct);
            if (!evidencia.EsExito) return Responder(evidencia);
            await evidencia.Valor!.contenido.DisposeAsync();
        }
        return Responder(await pagos.ResolverPagoPendienteAsync(AdministradorId, pedidoId, pagoId, solicitud, ct));
    }

    [HttpPost("{pedidoId:int:min(1)}/cobros"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> Cobrar(int pedidoId, RegistrarCobroDirectoDto solicitud, CancellationToken ct)
    {
        var resultado = await pagos.RegistrarCobroDirectoAsync(AdministradorId, pedidoId, solicitud, ct);
        if (resultado.EsExito && resultado.Valor!.esCreacionNueva)
            return StatusCode(StatusCodes.Status201Created, resultado.Valor);
        return Responder(resultado);
    }

    [HttpPost("{pedidoId:int:min(1)}/produccion"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> CambiarEstado(int pedidoId, ActualizarEstadoProduccionDto solicitud, CancellationToken ct) =>
        Responder(await produccion.ActualizarEstadoAsync(AdministradorId, pedidoId, solicitud, ct));

    [HttpPost("{pedidoId:int:min(1)}/sublimacion"), RequestSizeLimit(16_384)]
    public async Task<IActionResult> AutorizarSublimacion(int pedidoId, CancellationToken ct) =>
        Responder(await produccion.AutorizarSublimacionAsync(AdministradorId, pedidoId, ct));

    private IActionResult Descargar(ResultadoOperacionAdmin<ArchivoDescargaDto> resultado)
    {
        if (!resultado.EsExito) return Responder(resultado);
        var archivo = resultado.Valor!;
        Response.Headers.CacheControl = "no-store";
        return File(archivo.contenido, archivo.tipoContenido, archivo.nombreDescarga, enableRangeProcessing: false);
    }

    private IActionResult Responder<T>(ResultadoOperacionAdmin<T> resultado)
    {
        if (resultado.EsExito) return Ok(resultado.Valor);
        var error = resultado.Error!;
        var estado = error.codigo switch
        {
            CodigoErrorAdmin.Validacion => 400,
            CodigoErrorAdmin.PermisoDenegado => 403,
            CodigoErrorAdmin.NoEncontrado => 404,
            _ => 409
        };
        return StatusCode(estado, new ProblemDetails
        {
            Status = estado,
            Title = error.mensaje,
            Extensions = { ["campo"] = error.campo, ["codigo"] = error.codigo.ToString() }
        });
    }
}

public sealed class SubirDisenoAdminDto
{
    [Required] public IFormFile archivo { get; init; } = null!;
    [StringLength(1000)] public string? descripcion { get; init; }
}
