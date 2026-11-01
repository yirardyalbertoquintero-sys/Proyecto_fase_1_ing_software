using ImpactoTextil.Api.Repositories;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImpactoTextil.Api.Controllers;

[ApiController]
[Route("api/imagenes-catalogo")]
public sealed class ImagenCatalogoController(ImagenCatalogoStore imagenes, IProductoRepository productos) : ControllerBase
{
    [HttpPost, Authorize(Policy = "Administrador"), EnableRateLimiting("imagenes-catalogo")]
    [RequestSizeLimit(10_100_000), RequestFormLimits(MultipartBodyLengthLimit = 10_100_000)]
    public async Task<IActionResult> Subir(IFormFile? archivo, CancellationToken cancelacion)
    {
        if (archivo is null) return BadRequest(new ProblemDetails { Status = 400, Title = "Selecciona una foto de la prenda." });
        try { return Ok(new { imagenURL = await imagenes.GuardarAsync(archivo, cancelacion) }); }
        catch (ImagenCatalogoInvalidaException error)
        { return BadRequest(new ProblemDetails { Status = 400, Title = error.Message }); }
    }

    [HttpGet("{archivo}")]
    public async Task<IActionResult> Ver(string archivo, CancellationToken cancelacion)
    {
        var referencia = "/api/imagenes-catalogo/" + archivo;
        var ruta = imagenes.Ruta(referencia);
        if (ruta is null || !System.IO.File.Exists(ruta) || !await productos.UsaImagenAsync(referencia, cancelacion)) return NotFound();
        return PhysicalFile(ruta, archivo.EndsWith(".png", StringComparison.Ordinal) ? "image/png" : "image/jpeg");
    }
}
