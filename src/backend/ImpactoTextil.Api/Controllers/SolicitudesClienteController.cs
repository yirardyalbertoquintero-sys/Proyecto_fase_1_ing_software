using System.Security.Claims;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Controllers;

[ApiController, Authorize(Policy = "Cliente")]
[Route("api/solicitudes")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SolicitudesClienteController(SolicitudClienteService servicio) : ControllerBase
{
    [HttpPost, Consumes("multipart/form-data")]
    [RequestSizeLimit(RegistrarSolicitudDto.MaximoPeticion)]
    [RequestFormLimits(MultipartBodyLengthLimit = RegistrarSolicitudDto.MaximoPeticion, ValueLengthLimit = 131072)]
    public async Task<IActionResult> Registrar(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        if (form.Keys.Count() != 1 || !form.ContainsKey("datos") || form["datos"].Count != 1)
            return Invalido("Envía un único formulario de solicitud.");
        RegistrarSolicitudDto? datos;
        try { datos = JsonSerializer.Deserialize<RegistrarSolicitudDto>(form["datos"][0]!); }
        catch (JsonException) { return Invalido("Revisa los datos y campos de la solicitud."); }
        if (datos is null) return Invalido("Completa la solicitud.");
        if (!TryValidateModel(datos)) return ValidationProblem(ModelState);
        var cliente = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
        // Precondición de identidad del formulario; el dueño siempre procede de la sesión.
        if (datos.clienteConfirmado != cliente)
            return StatusCode(403, new ProblemDetails { Status = 403, Title = "La sesión cambió de cuenta. Revisa tu carrito antes de continuar." });
        var esperados = datos.detalles!.Where(d => d!.archivo is not null).Select(d => d!.archivo!).ToHashSet(StringComparer.Ordinal);
        var archivos = new Dictionary<string, IFormFile>(StringComparer.Ordinal);
        long totalBytes = 0;
        foreach (var f in form.Files)
        {
            if (!esperados.Contains(f.Name) || !archivos.TryAdd(f.Name, f)
                || f.Length is <= 0 or > ValidadorArchivosPrivados.MaximoBytesInclusivo)
                return Invalido("Cada diseño debe ser JPG/PNG válido de hasta 10 000 000 bytes, sin adjuntos repetidos o ajenos.");
            totalBytes += f.Length;
        }
        if (archivos.Count != esperados.Count || totalBytes > RegistrarSolicitudDto.MaximoArchivosTotales)
            return Invalido("Adjunta los diseños indicados sin superar 20 MB en conjunto.");
        ResultadoOperacionAdmin<SolicitudCreadaDto> resultado;
        try
        {
            resultado = await servicio.RegistrarAsync(cliente, datos, archivos, ct);
        }
        catch (InvalidDataException ex) { return Invalido(ex.Message); }
        if (resultado.EsExito) return StatusCode(resultado.Valor!.esCreacionNueva ? 201 : 200, resultado.Valor);
        var estado = resultado.Error!.codigo == CodigoErrorAdmin.PermisoDenegado ? 403 : 409;
        return StatusCode(estado, new ProblemDetails
        {
            Status = estado,
            Title = resultado.Error.mensaje,
            Extensions = { ["campo"] = resultado.Error.campo }
        });
    }
    private BadRequestObjectResult Invalido(string mensaje) => BadRequest(new ProblemDetails { Status = 400, Title = mensaje });
}
