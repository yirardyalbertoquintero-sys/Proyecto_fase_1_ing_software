using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Services;

namespace ImpactoTextil.Api.Repositories;

public sealed record ReferenciaArchivoDiseno(
    bool ExisteRegistro,
    string? ArchivoRelativo,
    string? DescripcionOIdea,
    string NombreSugerido);

public interface IPedidoAdministracionRepository
{
    Task<Usuario?> ObtenerAdministradorActivoAsync(int usuarioId, CancellationToken cancelacion);

    Task<IReadOnlyList<ResumenPedidoAdminDto>> ListarPedidosAsync(
        string? estado,
        string condicionAdministrativa,
        CancellationToken cancelacion);

    Task<ExpedientePedidoAdminDto?> ObtenerExpedienteAsync(
        int pedidoId,
        CancellationToken cancelacion);

    Task<ReferenciaArchivoDiseno> ObtenerReferenciaDisenoOriginalAsync(
        int pedidoId,
        int detalleId,
        CancellationToken cancelacion);

    Task<ReferenciaArchivoDiseno> ObtenerReferenciaVersionDisenoAsync(
        int pedidoId,
        int detalleId,
        int versionDisenoId,
        CancellationToken cancelacion);

    Task<ResultadoOperacionAdmin<T>> EjecutarEnPedidoBloqueadoAsync<T>(
        int pedidoId,
        Func<Pedido, Func<CancellationToken, Task>, CancellationToken, Task<ResultadoOperacionAdmin<T>>> operacion,
        CancellationToken cancelacion);
}
