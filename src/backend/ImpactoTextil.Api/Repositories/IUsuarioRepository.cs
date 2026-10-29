using ImpactoTextil.Api.Domain.Entities;

namespace ImpactoTextil.Api.Repositories;

public interface IUsuarioRepository
{
    // False significa correo duplicado; otros fallos se propagan al manejador de errores.
    Task<bool> IntentarCrearAsync(Usuario usuario, CancellationToken cancelacion);
}
