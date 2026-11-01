using ImpactoTextil.Api.Contracts;

namespace ImpactoTextil.Api.Repositories;

public interface IPedidoClienteRepository
{
    Task<IReadOnlyList<ResumenPedidoClienteDto>> ListarAsync(int cliente, CancellationToken ct);
    Task<bool> EsPropioAsync(int cliente, int pedido, CancellationToken ct);
    Task<ExpedientePedidoClienteDto?> ObtenerAsync(int cliente, int pedido, CancellationToken ct);
    Task<string?> ReferenciaComprobanteAsync(int cliente, int pedido, int pago, CancellationToken ct);
    Task<bool> ArchivoReferenciadoAsync(string referencia, CancellationToken ct);
}
