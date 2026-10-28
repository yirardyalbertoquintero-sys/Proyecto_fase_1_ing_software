using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ImpactoTextil.Api.Infrastructure;

public sealed class ManejadorErrores(ILogger<ManejadorErrores> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception error, CancellationToken cancelacion)
    {
        logger.LogError(error, "Error no controlado. Referencia {Referencia}", contexto.TraceIdentifier);
        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await contexto.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "No pudimos completar la operación.",
            Detail = "Inténtalo nuevamente. Si continúa, informa la referencia a la administración.",
            Extensions = { ["referencia"] = contexto.TraceIdentifier }
        }, cancellationToken: cancelacion);
        return true;
    }
}
