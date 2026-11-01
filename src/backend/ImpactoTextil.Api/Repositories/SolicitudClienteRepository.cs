using System.Data;
using System.Security.Cryptography;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Repositories;

public sealed class SolicitudClienteRepository(ImpactoTextilDbContext contexto) : ISolicitudClienteRepository
{
    public async Task<ResultadoOperacionAdmin<SolicitudCreadaDto>> RegistrarAsync(int clienteId, RegistrarSolicitudDto datos,
        byte[] huella, Func<IReadOnlyList<Producto>, CancellationToken, Task<ResultadoOperacionAdmin<Pedido>>> construir,
        CancellationToken cancelacion)
    {
        await using var tx = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelacion);
        // Serializar la confirmación por Cliente antes de leer su clave única.
        // Los productos se bloquean en orden estable, como la edición del catálogo.
        var usuario = await contexto.Usuario.FromSqlInterpolated(
            $"SELECT * FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK) WHERE id = {clienteId}")
            .AsNoTracking().SingleOrDefaultAsync(cancelacion);
        if (usuario is not { activo: true, rol: "Cliente" })
            return ResultadoOperacionAdmin<SolicitudCreadaDto>.Fallo(CodigoErrorAdmin.PermisoDenegado, "Necesitas una cuenta Cliente activa.");
        var previo = await contexto.Pedido.AsNoTracking().SingleOrDefaultAsync(
            p => p.cliente == clienteId && p.confirmacion == datos.confirmacion, cancelacion);
        if (previo is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(previo.huellaSolicitud, huella))
                return ResultadoOperacionAdmin<SolicitudCreadaDto>.Fallo(CodigoErrorAdmin.ConflictoEstado,
                    "Esa confirmación ya corresponde a otra solicitud. Revisa el carrito antes de confirmar de nuevo.");
            // Reintento antes de validar cambios posteriores del catálogo.
            var referencia = await contexto.DetallePedido.Where(d => d.pedido == previo.id)
                .SumAsync(d => d.precioBaseReferencia * d.cantidad, cancelacion);
            await tx.CommitAsync(cancelacion);
            return ResultadoOperacionAdmin<SolicitudCreadaDto>.Exito(Salida(previo, referencia, false));
        }
        var productos = new List<Producto>();
        foreach (var id in datos.detalles!.Select(d => d!.producto).Distinct().Order())
        {
            var p = await contexto.Producto.FromSqlInterpolated(
                $"SELECT * FROM dbo.Producto WITH (UPDLOCK, HOLDLOCK) WHERE id = {id}")
                .AsNoTracking().SingleOrDefaultAsync(cancelacion);
            if (p is not null) productos.Add(p);
        }
        var resultado = await construir(productos, cancelacion);
        if (!resultado.EsExito)
            return ResultadoOperacionAdmin<SolicitudCreadaDto>.Fallo(resultado.Error!.codigo, resultado.Error.mensaje, resultado.Error.campo);
        var pedido = resultado.Valor!;
        contexto.Add(pedido);
        await contexto.SaveChangesAsync(cancelacion);
        await tx.CommitAsync(cancelacion);
        return ResultadoOperacionAdmin<SolicitudCreadaDto>.Exito(
            Salida(pedido, pedido.DetallePedido.Sum(d => d.precioBaseReferencia * d.cantidad), true));
    }

    public async Task<bool> ArchivoReferenciadoAsync(string referencia, CancellationToken cancelacion) =>
        await contexto.DetallePedido.AsNoTracking().AnyAsync(d => d.archivoDisenoURL == referencia, cancelacion)
        || await contexto.VersionDiseno.AsNoTracking().AnyAsync(d => d.archivoURL == referencia, cancelacion);

    // SUM(decimal * int) puede devolver escala 4 en SQL; el contrato monetario usa centavos.
    private static SolicitudCreadaDto Salida(Pedido p, decimal total, bool nuevo) => new(p.id, p.codigo, p.estado, decimal.Round(total, 2), p.fechaOrden, nuevo);
}
