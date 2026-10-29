using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Repositories;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Controllers;

[ApiController, Authorize(Policy = "Administrador")]
[Route("api/administracion/productos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CatalogoAdministracionController(CatalogoService catalogo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancelacion) => Ok(await catalogo.ListarAsync(cancelacion));

    [HttpGet("{id:int:min(1)}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancelacion) =>
        await catalogo.ObtenerAsync(id, cancelacion) is { } producto ? Ok(producto) : NotFound();

    [HttpPost, RequestSizeLimit(16_384)]
    public Task<IActionResult> Crear(GuardarProductoDto datos, CancellationToken cancelacion) => Guardar(null, datos, cancelacion);

    [HttpPut("{id:int:min(1)}"), RequestSizeLimit(16_384)]
    public Task<IActionResult> Editar(int id, GuardarProductoDto datos, CancellationToken cancelacion) => Guardar(id, datos, cancelacion);

    private async Task<IActionResult> Guardar(int? id, GuardarProductoDto datos, CancellationToken cancelacion)
    {
        if (id is not null && datos.version is null)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Recarga la variante antes de editarla." });
        try
        {
            var producto = await catalogo.GuardarAsync(id, datos, cancelacion);
            if (producto is null) return NotFound();
            return id is null ? CreatedAtAction(nameof(Obtener), new { id = producto.id }, producto) : Ok(producto);
        }
        catch (ProductoModificadoException)
        { return Conflict(new ProblemDetails { Status = 409, Title = "Otro cambio actualizó esta variante. Recárgala y revisa sus datos antes de guardar." }); }
        catch (ImagenCatalogoInvalidaException error)
        { return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["imagenURL"] = [error.Message] }) { Status = 400, Title = "Revisa la imagen de la prenda." }); }
    }
}
