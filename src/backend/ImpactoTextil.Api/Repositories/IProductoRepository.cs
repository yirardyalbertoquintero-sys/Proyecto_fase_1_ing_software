using ImpactoTextil.Api.Domain.Entities;

namespace ImpactoTextil.Api.Repositories;

public interface IProductoRepository
{
    Task<IReadOnlyList<Producto>> ListarAsync(CancellationToken cancelacion);
    Task<Producto?> ObtenerAsync(int id, CancellationToken cancelacion);
    Task<IReadOnlyList<Producto>> ListarActivosAsync(CancellationToken cancelacion);
    Task<Producto?> ObtenerActivoAsync(int id, CancellationToken cancelacion);
    Task<Producto> CrearAsync(Producto producto, CancellationToken cancelacion);
    Task<Producto?> ActualizarAsync(int id, string version, Producto nuevos, CancellationToken cancelacion);
    Task<bool> UsaImagenAsync(string referencia, CancellationToken cancelacion);
}

public sealed class ProductoModificadoException : Exception;
