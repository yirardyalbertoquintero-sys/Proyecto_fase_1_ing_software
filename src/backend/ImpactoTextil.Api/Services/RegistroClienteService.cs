using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class RegistroClienteService(
    IUsuarioRepository usuarios, IProtectorContrasena protector, TimeProvider reloj)
{
    public async Task<ClienteCreadoDto?> RegistrarAsync(RegistrarClienteDto solicitud, CancellationToken cancelacion)
    {
        var usuario = new Usuario
        {
            nombre = solicitud.nombre!.Trim(),
            correo = solicitud.correo!.Trim().ToLowerInvariant(),
            passwordHash = protector.CrearHash(solicitud.contrasena!),
            rol = "Cliente",
            activo = true,
            aceptacionTerminos = true,
            fechaAceptacion = reloj.GetUtcNow()
        };
        return await usuarios.IntentarCrearAsync(usuario, cancelacion)
            ? new ClienteCreadoDto(usuario.id, usuario.rol)
            : null;
    }
}
