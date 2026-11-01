using System.Security.Claims;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Controllers;

[ApiController, Authorize(Policy = "Cliente"), Route("api/mis-pedidos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MisPedidosController(IPedidoClienteRepository repositorio,
    ComprobanteClienteService comprobantes, IAlmacenArchivosPrivados almacen) : ControllerBase
{
    private int Cliente => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
    private bool CuentaCoincide => !Request.Headers.TryGetValue("X-Cliente-Confirmado", out var cuenta)
        || int.TryParse(cuenta, out var id) && id == Cliente;
    private ObjectResult CuentaDistinta() => StatusCode(403, new ProblemDetails { Status = 403, Title = "La cuenta cambió. Actualiza tu sesión para continuar." });
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => CuentaCoincide ? Ok(await repositorio.ListarAsync(Cliente, ct)) : CuentaDistinta();
    [HttpGet("{pedidoId:int:min(1)}")]
    public async Task<IActionResult> Obtener(int pedidoId, CancellationToken ct)
    {
        if (!CuentaCoincide) return CuentaDistinta();
        var e = await repositorio.ObtenerAsync(Cliente, pedidoId, ct);
        return e is null ? NoEncontrado() : Ok(e);
    }
    [HttpGet("{pedidoId:int:min(1)}/pagos/{pagoId:int:min(1)}/comprobante")]
    public async Task<IActionResult> Descargar(int pedidoId, int pagoId, CancellationToken ct)
    {
        if (!CuentaCoincide) return CuentaDistinta();
        var referencia = await repositorio.ReferenciaComprobanteAsync(Cliente, pedidoId, pagoId, ct);
        if (referencia is null) return NoEncontrado();
        var a = await almacen.AbrirLecturaValidadaAsync(referencia, TipoArchivoPrivado.ComprobantePago, ct);
        if (a is null) return NoEncontrado();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(a.Contenido, a.TipoContenido, $"comprobante-pedido-{pedidoId}-pago-{pagoId}{a.ExtensionNormalizada}");
    }
    [HttpPost("{pedidoId:int:min(1)}/comprobantes"), Consumes("multipart/form-data")]
    [RequestSizeLimit(10_050_000), RequestFormLimits(MultipartBodyLengthLimit = 10_050_000, ValueLengthLimit = 16384)]
    public async Task<IActionResult> Registrar(int pedidoId, CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        if (form.Keys.Count() != 1 || !form.ContainsKey("datos") || form["datos"].Count != 1
            || form.Files.Count != 1 || form.Files[0].Name != "archivo") return Invalido("Envía los datos y un único comprobante.");
        RegistrarComprobanteDto? datos;
        try { datos = JsonSerializer.Deserialize<RegistrarComprobanteDto>(form["datos"][0]!); }
        catch (JsonException) { return Invalido("Revisa los datos del comprobante."); }
        if (datos is null) return Invalido("Completa los datos del comprobante.");
        if (!TryValidateModel(datos)) return ValidationProblem(ModelState);
        if (datos.clienteConfirmado != Cliente) return StatusCode(403, new ProblemDetails { Status = 403, Title = "La cuenta cambió. Accede con la cuenta que preparó este envío." });
        var archivo = form.Files[0];
        if (archivo.Length is <= 0 or > ValidadorArchivosPrivados.MaximoBytesInclusivo)
            return Invalido("Adjunta un JPG, PNG o PDF de hasta 10 000 000 bytes.");
        try
        {
            var r = await comprobantes.RegistrarAsync(Cliente, pedidoId, datos, archivo, ct);
            if (r.EsExito) return StatusCode(r.Valor!.esCreacionNueva ? 201 : 200, r.Valor);
            var status = r.Error!.codigo == CodigoErrorAdmin.NoEncontrado ? 404 : 409;
            return StatusCode(status, new ProblemDetails { Status = status, Title = r.Error.mensaje });
        }
        catch (InvalidDataException ex) { return Invalido(ex.Message); }
    }
    private NotFoundObjectResult NoEncontrado() => NotFound(new ProblemDetails { Status = 404, Title = "No encontramos ese registro en tu cuenta." });
    private BadRequestObjectResult Invalido(string texto) => BadRequest(new ProblemDetails { Status = 400, Title = texto });
}
