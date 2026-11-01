using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Repositories;

public sealed class UsuarioRepository(ImpactoTextilDbContext contexto) : IUsuarioRepository
{
    public Task<Usuario?> BuscarPorCorreoAsync(string correo, CancellationToken cancelacion) =>
        contexto.Usuario.AsNoTracking().SingleOrDefaultAsync(u => u.correo == correo, cancelacion);

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancelacion) =>
        contexto.Usuario.AsNoTracking().SingleOrDefaultAsync(u => u.id == id, cancelacion);

    public async Task<bool> IntentarCrearAsync(Usuario usuario, CancellationToken cancelacion)
    {
        contexto.Set<Usuario>().Add(usuario);
        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return true;
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // La restricción única resuelve también dos registros simultáneos del mismo correo.
            contexto.Entry(usuario).State = EntityState.Detached;
            return false;
        }
    }
}
