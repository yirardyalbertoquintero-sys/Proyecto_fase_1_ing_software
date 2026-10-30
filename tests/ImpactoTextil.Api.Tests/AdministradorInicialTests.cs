using System.ComponentModel.DataAnnotations;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Tests;

public sealed class AdministradorInicialTests(BaseSqlPruebas datos) : IClassFixture<BaseSqlPruebas>
{
    [PruebaSql]
    public async Task InicializacionControlada_ValidaNoPromueveClientesYResisteDosCreacionesSimultaneas()
    {
        const string clave = "contraseña ficticia de inicialización";
        var protector = new ProtectorContrasena();
        await using (var contexto = datos.CrearContexto())
        {
            var servicio = new AdministradorInicialService(contexto, protector);
            await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearAsync("Prueba", "admin@example.test", "corta", default));
            Assert.Empty(await contexto.Usuario.ToListAsync());
            contexto.Usuario.Add(new Usuario { nombre = "Cliente ficticio", correo = "cliente@example.test", passwordHash = protector.CrearHash(clave),
                activo = true, rol = "Cliente", aceptacionTerminos = true, fechaAceptacion = DateTimeOffset.UtcNow });
            await contexto.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync("Administrador", "CLIENTE@example.test", clave, default));
            Assert.Equal("Cliente", (await contexto.Usuario.SingleAsync()).rol);
        }
        async Task<bool> Intentar(string correo)
        {
            await using var contexto = datos.CrearContexto();
            try { await new AdministradorInicialService(contexto, protector).CrearAsync("Administrador ficticio", correo, clave, default); return true; }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Ya existe")) { return false; }
        }
        var resultados = await Task.WhenAll(Intentar("primero@example.test"), Intentar("segundo@example.test"));
        Assert.Equal(1, resultados.Count(r => r));
        await using var comprobacion = datos.CrearContexto();
        var admin = await comprobacion.Usuario.SingleAsync(u => u.rol == "Administrador");
        Assert.True(admin.activo);
        Assert.False(admin.aceptacionTerminos);
        Assert.Null(admin.fechaAceptacion);
        Assert.True(protector.Verificar(clave, admin.passwordHash));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new AdministradorInicialService(comprobacion, protector)
            .CrearAsync("Otro", "otro@example.test", clave, default));
        Assert.Equal(2, await comprobacion.Usuario.CountAsync());
    }
}
