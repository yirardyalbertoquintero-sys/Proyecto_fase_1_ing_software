using System.ComponentModel.DataAnnotations;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Services;

public sealed class AdministradorInicialService(ImpactoTextilDbContext contexto, IProtectorContrasena protector)
{
    public async Task<int> CrearAsync(string nombre, string correo, string contrasena, CancellationToken cancelacion)
    {
        // Reutilizar las reglas confirmadas sin atribuir al administrador un consentimiento no recibido.
        var validacion = new RegistrarClienteDto { nombre = nombre, correo = correo, contrasena = contrasena, aceptacionTerminos = true };
        Validator.ValidateObject(validacion, new ValidationContext(validacion), validateAllProperties: true);
        var hash = protector.CrearHash(contrasena);
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);
        await contexto.Database.ExecuteSqlRawAsync("""
            DECLARE @resultado int;
            EXEC @resultado = sys.sp_getapplock @Resource=N'ImpactoTextil:PrimerAdministrador',
                @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=5000;
            IF @resultado < 0 THROW 51000, 'Inicializacion ocupada.', 1;
            """, cancelacion);
        if (await contexto.Usuario.AnyAsync(u => u.rol == "Administrador", cancelacion))
            throw new InvalidOperationException("Ya existe un Administrador. Este comando solo crea el primero.");
        var normalizado = correo.Trim().ToLowerInvariant();
        if (await contexto.Usuario.AnyAsync(u => u.correo == normalizado, cancelacion))
            throw new InvalidOperationException("Ese correo ya está registrado. El comando no cambia roles ni contraseñas existentes.");
        var usuario = new Usuario { nombre = nombre.Trim(), correo = normalizado, passwordHash = hash,
            rol = "Administrador", activo = true, aceptacionTerminos = false, fechaAceptacion = null };
        contexto.Usuario.Add(usuario);
        await contexto.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return usuario.id;
    }
}
