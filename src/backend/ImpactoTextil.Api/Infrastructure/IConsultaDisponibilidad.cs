namespace ImpactoTextil.Api.Infrastructure;

public interface IConsultaDisponibilidad
{
    Task<bool> ComprobarAsync(CancellationToken cancelacion);
}
