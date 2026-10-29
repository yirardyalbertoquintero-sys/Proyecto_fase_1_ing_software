using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Repositories;

public sealed class ProductoRepository(ImpactoTextilDbContext contexto) : IProductoRepository
{
    public async Task<IReadOnlyList<Producto>> ListarAsync(CancellationToken cancelacion) =>
        await contexto.Producto.AsNoTracking().OrderBy(p => p.nombre).ThenBy(p => p.id).ToListAsync(cancelacion);

    public Task<Producto?> ObtenerAsync(int id, CancellationToken cancelacion) =>
        contexto.Producto.AsNoTracking().SingleOrDefaultAsync(p => p.id == id, cancelacion);

    public async Task<Producto> CrearAsync(Producto producto, CancellationToken cancelacion)
    {
        contexto.Producto.Add(producto);
        await contexto.SaveChangesAsync(cancelacion);
        return producto;
    }

    public async Task<Producto?> ActualizarAsync(int id, string version, Producto nuevos, CancellationToken cancelacion)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);
        // Bloqueo de esta fila hasta confirmar; la huella evita sobrescribir una edición ya guardada.
        var actual = await contexto.Producto.FromSqlInterpolated(
            $"SELECT * FROM [Producto] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {id}")
            .SingleOrDefaultAsync(cancelacion);
        if (actual is null) return null;
        if (!string.Equals(ProductoVersion.Calcular(actual), version, StringComparison.Ordinal))
            throw new ProductoModificadoException();
        actual.nombre = nuevos.nombre; actual.color = nuevos.color; actual.talla = nuevos.talla;
        actual.tela = nuevos.tela; actual.imagenURL = nuevos.imagenURL;
        actual.precioBase = nuevos.precioBase; actual.activo = nuevos.activo;
        await contexto.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return actual;
    }

    public Task<bool> UsaImagenAsync(string referencia, CancellationToken cancelacion) =>
        contexto.Producto.AnyAsync(p => p.imagenURL == referencia, cancelacion);
}
