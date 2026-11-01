using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Infrastructure;

public sealed class ConsultaDisponibilidadSql(
    ImpactoTextilDbContext contexto,
    IConfiguration configuracion,
    ILogger<ConsultaDisponibilidadSql> logger) : IConsultaDisponibilidad
{
    public async Task<bool> ComprobarAsync(CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(configuracion.GetConnectionString("ImpactoTextil")))
            return false;

        try
        {
            return await contexto.Database.CanConnectAsync(cancelacion);
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "No se pudo comprobar la conexión con SQL Server.");
            return false;
        }
    }
}
