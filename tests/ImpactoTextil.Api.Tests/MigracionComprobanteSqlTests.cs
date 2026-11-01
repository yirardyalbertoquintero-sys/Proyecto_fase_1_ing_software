using ImpactoTextil.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ImpactoTextil.Api.Tests;

public sealed class MigracionComprobanteSqlTests
{
    [PruebaSql]
    public async Task ActualizarBaseExistente_ConservaPagoResueltoSinRellenarHuella()
    {
        // Base exclusiva: no degradar ni modificar la base de otro fixture o del usuario.
        var datos = new BaseSqlPruebas(); await datos.InitializeAsync();
        try
        {
            int pago;
            await using (var c = datos.CrearContexto())
            {
                var u = new Usuario { nombre = "Persona QA", correo = "legado@example.invalid", rol = "Cliente", activo = true, passwordHash = new string('x', 60), aceptacionTerminos = true, fechaAceptacion = DateTimeOffset.UtcNow };
                u.passwordHash = "$2b$" + new string('x', 56);
                var a = new Usuario { nombre = "Admin QA", correo = "admin-legado@example.invalid", rol = "Administrador", activo = true, passwordHash = u.passwordHash };
                c.AddRange(u, a); await c.SaveChangesAsync();
                var p = new Pedido { cliente = u.id, codigo = "QA-LEGADO", fechaOrden = DateTimeOffset.UtcNow, estado = "Pendiente", condicionAdministrativa = "Activa", modalidadEntrega = "Retiro", confirmacion = Guid.NewGuid(), huellaSolicitud = new byte[32] };
                c.Add(p); await c.SaveChangesAsync();
                var g = new Pago { pedido = p.id, monto = 100m, fecha = DateTimeOffset.UtcNow, medio = "Efectivo", referencia = "Constancia histórica", estadoVerificacion = "Verificado", fechaVerificacion = DateTimeOffset.UtcNow, responsableVerificacion = a.id, confirmacion = Guid.NewGuid() };
                c.Add(g); await c.SaveChangesAsync(); pago = g.id;
                await c.GetService<IMigrator>().MigrateAsync("20261028165048_Inicial");
                Assert.Equal(1, await c.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM dbo.Pago WHERE estadoVerificacion=N'Verificado'").SingleAsync());
                await c.Database.MigrateAsync();
            }
            await using (var c = datos.CrearContexto())
            {
                var g = await c.Pago.AsNoTracking().SingleAsync(g => g.id == pago);
                Assert.Null(g.huellaComprobante); Assert.Equal("Verificado", g.estadoVerificacion); Assert.Equal("Constancia histórica", g.referencia); Assert.Equal(100m, g.monto);
                var antes = (await c.Database.GetAppliedMigrationsAsync()).ToArray(); await c.Database.MigrateAsync(); Assert.Equal(antes, (await c.Database.GetAppliedMigrationsAsync()).ToArray());
            }
        }
        finally { await datos.DisposeAsync(); }
    }
}
