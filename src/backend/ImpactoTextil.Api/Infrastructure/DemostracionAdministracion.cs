using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Infrastructure;

/// <summary>Herramienta local explícita; no es registro de pedidos para el cliente.</summary>
public static class DemostracionAdministracion
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAAAE0lEQVR4nGP88PoOAwwwwVl4OQCI1gK/rni6WQAAAABJRU5ErkJggg==");

    public static async Task<int> EjecutarAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var c = scope.ServiceProvider.GetRequiredService<ImpactoTextilDbContext>();
        var admin = await c.Usuario.FirstOrDefaultAsync(u => u.rol == "Administrador" && u.activo);
        if (admin is null)
        {
            Console.Error.WriteLine("Crea primero el Administrador mediante la tarea de VS Code correspondiente.");
            return 1;
        }
        var codigos = new[] { "DEMO-ADMIN-01", "DEMO-ADMIN-02", "DEMO-ADMIN-03" };
        var existentes = await c.Pedido.Where(p => codigos.Contains(p.codigo)).Select(p => p.codigo).ToListAsync();
        if (existentes.Count == codigos.Length)
        {
            Console.WriteLine("Los tres pedidos de demostración ya existen. Se conservan sus cambios y decisiones.");
            return 0;
        }
        var almacen = scope.ServiceProvider.GetRequiredService<IAlmacenArchivosPrivados>();
        await using var tx = await c.Database.BeginTransactionAsync();
        var cliente = new Usuario
        {
            nombre = "Cliente ficticio de demostración",
            correo = $"demo-{Guid.NewGuid():N}@example.invalid",
            passwordHash = scope.ServiceProvider.GetRequiredService<IProtectorContrasena>().CrearHash(Guid.NewGuid().ToString("N")),
            rol = "Cliente",
            activo = false,
            aceptacionTerminos = true,
            fechaAceptacion = DateTimeOffset.UtcNow
        };
        var producto = new Producto
        {
            nombre = "Prenda ficticia administrativa",
            talla = "M",
            color = "Blanco",
            tela = "Poliéster",
            imagenURL = "/imagen-demostracion-no-disponible.png",
            precioBase = 150m,
            activo = false
        };
        c.AddRange(cliente, producto); await c.SaveChangesAsync();
        foreach (var codigo in codigos.Except(existentes))
        {
            var aceptado = codigo == "DEMO-ADMIN-02";
            var sinAcuerdo = codigo == "DEMO-ADMIN-03";
            string? original = null;
            if (!sinAcuerdo)
            {
                await using var imagen = new MemoryStream(Png);
                original = await almacen.GuardarAsync(imagen, TipoArchivoPrivado.DisenoCliente, CancellationToken.None);
            }
            var pedido = new Pedido
            {
                codigo = codigo,
                cliente = cliente.id,
                fechaOrden = DateTimeOffset.UtcNow,
                estado = "Pendiente",
                condicionAdministrativa = "Activa",
                modalidadEntrega = "Retiro",
                confirmacion = Guid.NewGuid(),
                huellaSolicitud = new byte[32]
            };
            c.Add(pedido); await c.SaveChangesAsync();
            var detalle = new DetallePedido
            {
                pedido = pedido.id,
                producto = producto.id,
                cantidad = 2,
                talla = "M",
                color = "Blanco",
                tela = "Poliéster",
                empaque = "Sin empaque",
                ideaDiseno = sinAcuerdo ? "Sin personalización" : "Diseño ficticio para evaluación",
                archivoDisenoURL = original,
                precioBaseReferencia = 150m
            };
            c.Add(detalle); await c.SaveChangesAsync();
            var ahora = DateTimeOffset.UtcNow;
            var diseno = new VersionDiseno
            {
                detalle = detalle.id,
                numeroVersion = 1,
                archivoURL = original,
                descripcion = "Versión ficticia de demostración",
                fechaRegistro = ahora,
                responsableRegistro = admin.id,
                decision = "Pendiente"
            };
            c.Add(diseno); await c.SaveChangesAsync();
            if (sinAcuerdo) continue;
            var cotizacion = new VersionCotizacion { pedido = pedido.id, numeroVersion = 1, fechaRegistro = ahora, responsableRegistro = admin.id, decision = "Propuesta" };
            c.Add(cotizacion); await c.SaveChangesAsync();
            c.Add(new DetalleCotizacion { versionCotizacion = cotizacion.id, detalle = detalle.id, precioUnitarioAcordado = 500m, versionDiseno = diseno.id });
            await c.SaveChangesAsync();
            if (!aceptado) continue;
            diseno.decision = "Aprobada"; diseno.fechaDecision = ahora; diseno.responsableDecision = admin.id; diseno.referenciaDecision = "Aprobación ficticia de demostración";
            cotizacion.decision = "Aceptada"; cotizacion.fechaDecision = ahora; cotizacion.responsableDecision = admin.id; cotizacion.referenciaDecision = "Acuerdo ficticio de demostración";
            await c.SaveChangesAsync();
            detalle.versionDisenoVigente = diseno.id; pedido.versionCotizacionVigente = cotizacion.id;
            pedido.fechaEntregaAcordada = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
            await c.SaveChangesAsync();
            await using var evidencia = new MemoryStream(Png);
            var comprobante = await almacen.GuardarAsync(evidencia, TipoArchivoPrivado.ComprobantePago, CancellationToken.None);
            c.Add(new Pago
            {
                pedido = pedido.id,
                monto = 500m,
                fecha = ahora,
                medio = "Transferencia",
                referencia = "Evidencia ficticia DEMO-ADMIN-02",
                estadoVerificacion = "Pendiente",
                comprobanteURL = comprobante,
                confirmacion = Guid.NewGuid()
            });
            await c.SaveChangesAsync();
        }
        await tx.CommitAsync();
        Console.WriteLine("Pedidos ficticios preparados: DEMO-ADMIN-01 (revisión), 02 (comprobante pendiente), 03 (archivo). No se reemplazaron pedidos existentes.");
        return 0;
    }
}
