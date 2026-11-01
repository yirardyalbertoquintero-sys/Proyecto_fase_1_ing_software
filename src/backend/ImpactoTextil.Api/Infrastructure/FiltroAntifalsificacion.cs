using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ImpactoTextil.Api.Infrastructure;

public sealed class FiltroAntifalsificacion(IAntiforgery antifalsificacion) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        var metodo = contexto.HttpContext.Request.Method;
        if (HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo))
            return;
        try
        {
            await antifalsificacion.ValidateRequestAsync(contexto.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            contexto.Result = new ObjectResult(new ProblemDetails
            {
                Status = 400,
                Title = "La protección del formulario venció. Recarga la página e inténtalo nuevamente.",
                Extensions = { ["codigo"] = "formulario_vencido" }
            }) { StatusCode = 400 };
        }
    }
}
