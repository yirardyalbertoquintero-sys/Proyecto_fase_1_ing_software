using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Controllers;

[ApiController, AllowAnonymous]
[Route("api/productos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProductosController(CatalogoService catalogo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancelacion) => Ok(await catalogo.ListarPublicosAsync(cancelacion));

    [HttpGet("{id:int:min(1)}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancelacion) =>
        await catalogo.ObtenerPublicoAsync(id, cancelacion) is { } producto ? Ok(producto)
            : NotFound(new ProblemDetails { Status = 404, Title = "Esta variante no está disponible. Actualiza el catálogo para ver las prendas disponibles." });
}
