using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});
builder.Services.AddControllers(opciones => opciones.Filters.Add<FiltroAntifalsificacion>())
    .ConfigureApiBehaviorOptions(opciones => opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var errores = contexto.ModelState.Where(p => p.Value?.Errors.Count > 0)
            .ToDictionary(p => p.Key.StartsWith('$') ? "formulario" : p.Key,
                p => p.Value!.Errors.Select(e => e.Exception is not null || p.Key.StartsWith('$')
                    ? "Envía únicamente los campos del formulario con sus tipos correctos."
                    : e.ErrorMessage).ToArray());
        return new BadRequestObjectResult(new ValidationProblemDetails(errores)
        { Status = 400, Title = "Revisa los datos del formulario." });
    });
builder.Services.AddAntiforgery(opciones =>
{
    opciones.HeaderName = "X-CSRF-TOKEN";
    opciones.Cookie.Name = "__Host-ImpactoTextil.Antiforgery";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddRateLimiter(opciones =>
{
    opciones.AddPolicy("registro", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opciones.OnRejected = async (contexto, cancelacion) =>
    {
        contexto.HttpContext.Response.StatusCode = 429;
        contexto.HttpContext.Response.Headers.RetryAfter = "60";
        await contexto.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        { Status = 429, Title = "Has hecho varios intentos. Espera un minuto y vuelve a intentarlo." }, cancelacion);
    };
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.AddDbContext<ImpactoTextilDbContext>(opciones =>
{
    var conexion = builder.Configuration.GetConnectionString("ImpactoTextil");
    if (!string.IsNullOrWhiteSpace(conexion))
        opciones.UseSqlServer(conexion, sql => sql.CommandTimeout(5));
});
builder.Services.AddScoped<IConsultaDisponibilidad, ConsultaDisponibilidadSql>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<RegistroClienteService>();
builder.Services.AddSingleton<IProtectorContrasena, ProtectorContrasena>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.Use(async (contexto, siguiente) =>
{
    contexto.Response.Headers.XContentTypeOptions = "nosniff";
    contexto.Response.Headers["Referrer-Policy"] = "same-origin";
    contexto.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    await siguiente();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.MapControllers();
app.Run();

public partial class Program;
