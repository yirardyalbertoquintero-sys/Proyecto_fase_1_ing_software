using ImpactoTextil.Api.Domain;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class CatalogoService(IProductoRepository productos, ImagenCatalogoStore imagenes)
{
    public static ProductoDto Salida(Producto p) => new(p.id, p.nombre, p.color, p.talla, p.tela,
        p.imagenURL, p.precioBase, p.activo, ProductoVersion.Calcular(p));

    public async Task<IReadOnlyList<ProductoDto>> ListarAsync(CancellationToken cancelacion) =>
        (await productos.ListarAsync(cancelacion)).Select(Salida).ToArray();

    public async Task<ProductoDto?> ObtenerAsync(int id, CancellationToken cancelacion) =>
        await productos.ObtenerAsync(id, cancelacion) is { } p ? Salida(p) : null;

    private ProductoPublicoDto SalidaPublica(Producto p) => new(p.id, p.nombre, p.color, p.talla, p.tela,
        imagenes.Ruta(p.imagenURL) is not null ? p.imagenURL : null, p.precioBase);

    public async Task<IReadOnlyList<ProductoPublicoDto>> ListarPublicosAsync(CancellationToken cancelacion) =>
        (await productos.ListarActivosAsync(cancelacion)).Select(SalidaPublica).ToArray();

    public async Task<ProductoPublicoDto?> ObtenerPublicoAsync(int id, CancellationToken cancelacion) =>
        await productos.ObtenerActivoAsync(id, cancelacion) is { } p ? SalidaPublica(p) : null;

    public async Task<ProductoDto?> GuardarAsync(int? id, GuardarProductoDto datos, CancellationToken cancelacion)
    {
        if (!imagenes.Existe(datos.imagenURL!)) throw new ImagenCatalogoInvalidaException("Sube una imagen válida antes de guardar la variante.");
        var producto = new Producto { nombre = datos.nombre!.Trim(), color = datos.color!.Trim(),
            talla = datos.talla!.Trim(), tela = datos.tela!.Trim(), imagenURL = datos.imagenURL!,
            precioBase = datos.precioBase!.Value, activo = datos.activo!.Value };
        var guardado = id is { } valor
            ? await productos.ActualizarAsync(valor, datos.version!, producto, cancelacion)
            : await productos.CrearAsync(producto, cancelacion);
        return guardado is null ? null : Salida(guardado);
    }
}
