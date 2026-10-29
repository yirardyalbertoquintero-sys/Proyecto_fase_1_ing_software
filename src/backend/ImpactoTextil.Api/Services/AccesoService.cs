using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class AccesoService(IUsuarioRepository usuarios, IProtectorContrasena protector)
{
    // Igualar el trabajo de BCrypt evita una respuesta rápida que delate un correo inexistente.
    private static readonly string hashAusente = BCrypt.Net.BCrypt.HashPassword(
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)), workFactor: 12);

    public async Task<Usuario?> AutenticarAsync(IniciarSesionDto solicitud, CancellationToken cancelacion)
    {
        var usuario = await usuarios.BuscarPorCorreoAsync(solicitud.correo!.Trim().ToLowerInvariant(), cancelacion);
        var coincide = protector.Verificar(solicitud.contrasena!, usuario?.passwordHash ?? hashAusente);
        return coincide && usuario is { activo: true, rol: "Cliente" or "Administrador" } ? usuario : null;
    }
}
