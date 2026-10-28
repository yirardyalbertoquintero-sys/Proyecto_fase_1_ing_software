using ImpactoTextil.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.AddDbContext<ImpactoTextilDbContext>(opciones =>
{
    var conexion = builder.Configuration.GetConnectionString("ImpactoTextil");
    if (!string.IsNullOrWhiteSpace(conexion))
        opciones.UseSqlServer(conexion, sql => sql.CommandTimeout(5));
});
builder.Services.AddScoped<IConsultaDisponibilidad, ConsultaDisponibilidadSql>();

var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program;
