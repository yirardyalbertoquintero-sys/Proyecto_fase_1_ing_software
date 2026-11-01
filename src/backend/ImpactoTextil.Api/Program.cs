using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;
using ImpactoTextil.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;

var crearAdministrador = args.Contains("--crear-administrador", StringComparer.Ordinal);
var crearDemostracion = args.Contains("--crear-pedidos-demostracion", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args.Where(a => a is not ("--crear-administrador" or "--crear-pedidos-demostracion")).ToArray(),
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
    opciones.AddPolicy("acceso", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opciones.AddPolicy("imagenes-catalogo", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
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
builder.Services.AddScoped<AdministradorInicialService>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<IPedidoAdministracionRepository, PedidoAdministracionRepository>();
builder.Services.AddScoped<RevisionCotizacionAdminService>();
builder.Services.AddScoped<DescargaDisenoAdminService>();
builder.Services.AddScoped<DescargaComprobanteAdminService>();
builder.Services.AddScoped<PagosAdminService>();
builder.Services.AddScoped<ProduccionEntregaAdminService>();
builder.Services.AddScoped<ISolicitudClienteRepository, SolicitudClienteRepository>();
builder.Services.AddScoped<SolicitudClienteService>();
builder.Services.AddScoped<IPedidoClienteRepository, PedidoClienteRepository>();
builder.Services.AddScoped<ComprobanteClienteService>();
builder.Services.AddSingleton<IAlmacenArchivosPrivados>(servicios =>
{
    var entorno = servicios.GetRequiredService<IWebHostEnvironment>();
    var configuracion = servicios.GetRequiredService<IConfiguration>();
    return new AlmacenArchivosPrivadosLocal(configuracion["ArchivosPrivados:Carpeta"]
        ?? Path.Combine(entorno.ContentRootPath, "storage", "privado"));
});
builder.Services.AddSingleton<ImagenCatalogoStore>();
builder.Services.AddSingleton<IProtectorContrasena, ProtectorContrasena>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AlmacenSesiones>();
builder.Services.AddScoped<AccesoService>();
builder.Services.AddScoped<ValidarSesion>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(opciones =>
{
    opciones.Cookie.Name = "__Host-ImpactoTextil.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
    opciones.SlidingExpiration = true;
    opciones.EventsType = typeof(ValidarSesion);
});
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<AlmacenSesiones, TimeProvider>((opciones, almacen, reloj) =>
    {
        var minutos = builder.Configuration.GetValue<int>("Sesion:MinutosInactividad");
        if (minutos is < 1 or > 1440)
            throw new InvalidOperationException("Configura la duración de sesión acordada, entre 1 y 1440 minutos.");
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(minutos);
        opciones.SessionStore = almacen;
        opciones.TimeProvider = reloj;
    });
builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("Cliente", politica => politica.RequireAuthenticatedUser().RequireRole("Cliente"));
    opciones.AddPolicy("Administrador", politica => politica.RequireAuthenticatedUser().RequireRole("Administrador"));
});

var app = builder.Build();
if (crearDemostracion)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("La preparación de datos ficticios solo está habilitada en Development.");
        Environment.ExitCode = 1;
        return;
    }
    Environment.ExitCode = await DemostracionAdministracion.EjecutarAsync(app.Services);
    return;
}
if (crearAdministrador)
{
    Environment.ExitCode = await InicializarAdministrador.EjecutarAsync(app.Services);
    return;
}
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.Use(async (contexto, siguiente) =>
{
    contexto.Response.Headers.XContentTypeOptions = "nosniff";
    contexto.Response.Headers["Referrer-Policy"] = "same-origin";
    contexto.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' blob:; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    await siguiente();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
