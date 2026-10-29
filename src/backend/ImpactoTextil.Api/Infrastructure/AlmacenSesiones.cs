using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ImpactoTextil.Api.Infrastructure;

// Una instancia del servidor: reiniciarlo cierra las sesiones. Nunca se guardan contraseñas aquí.
public sealed class AlmacenSesiones(TimeProvider reloj) : ITicketStore
{
    private readonly ConcurrentDictionary<string, AuthenticationTicket> sesiones = new();

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        foreach (var (clave, valor) in sesiones)
            if (valor.Properties.ExpiresUtc is not { } fin || fin <= reloj.GetUtcNow())
                sesiones.TryRemove(new KeyValuePair<string, AuthenticationTicket>(clave, valor));
        var id = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        sesiones[id] = ticket;
        return Task.FromResult(id);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        // No resucitar una sesión que un cierre concurrente ya haya eliminado.
        if (sesiones.TryGetValue(key, out var anterior)) sesiones.TryUpdate(key, ticket, anterior);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        while (sesiones.TryGetValue(key, out var ticket))
        {
            if (ticket.Properties.ExpiresUtc > reloj.GetUtcNow())
                return Task.FromResult<AuthenticationTicket?>(ticket);
            // Si otra petición renovó el ticket, releerlo antes de decidir que venció.
            if (sesiones.TryRemove(new KeyValuePair<string, AuthenticationTicket>(key, ticket))) break;
        }
        return Task.FromResult<AuthenticationTicket?>(null);
    }

    public Task RemoveAsync(string key)
    {
        sesiones.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
