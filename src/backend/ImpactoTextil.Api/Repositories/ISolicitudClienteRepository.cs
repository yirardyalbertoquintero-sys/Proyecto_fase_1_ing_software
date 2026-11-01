using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Services;

namespace ImpactoTextil.Api.Repositories;

public interface ISolicitudClienteRepository
{
    Task<ResultadoOperacionAdmin<SolicitudCreadaDto>> RegistrarAsync(int clienteId, RegistrarSolicitudDto datos, byte[] huella,
        Func<IReadOnlyList<Producto>, CancellationToken, Task<ResultadoOperacionAdmin<Pedido>>> construir,
        CancellationToken cancelacion);
    Task<bool> ArchivoReferenciadoAsync(string referencia, CancellationToken cancelacion);
}
