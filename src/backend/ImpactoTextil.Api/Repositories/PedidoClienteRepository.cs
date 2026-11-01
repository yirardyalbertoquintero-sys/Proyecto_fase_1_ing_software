using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Repositories;

public sealed class PedidoClienteRepository(ImpactoTextilDbContext contexto,
    IPedidoAdministracionRepository expedientes) : IPedidoClienteRepository
{
    public async Task<IReadOnlyList<ResumenPedidoClienteDto>> ListarAsync(int cliente, CancellationToken ct)
    {
        var filas = await contexto.vPedido.AsNoTracking().Where(p => p.cliente == cliente)
            .OrderByDescending(p => p.fechaOrden).ThenByDescending(p => p.id).ToListAsync(ct);
        return filas.Select(p => new ResumenPedidoClienteDto(p.id, p.codigo, p.fechaOrden, p.estado,
            p.condicionAdministrativa, p.modalidadEntrega, p.fechaEntregaAcordada, p.fechaEntregaReal,
            p.cotizacionAceptada ?? false, p.totalReferencial ?? 0m, p.total, p.anticipoExigible, p.saldoPendiente)).ToList();
    }
    public Task<bool> EsPropioAsync(int cliente, int pedido, CancellationToken ct) =>
        contexto.Pedido.AsNoTracking().AnyAsync(p => p.id == pedido && p.cliente == cliente, ct);
    public async Task<ExpedientePedidoClienteDto?> ObtenerAsync(int cliente, int pedido, CancellationToken ct)
    {
        if (!await EsPropioAsync(cliente, pedido, ct)) return null;
        // La titularidad del Pedido es inmutable; el repositorio existente lee el expediente bajo bloqueo.
        var e = await expedientes.ObtenerExpedienteAsync(pedido, ct);
        if (e is null || e.pedido.clienteId != cliente) return null;
        var p = e.pedido;
        return new(new(p.id, p.codigo, p.fechaOrden, p.estado, p.condicionAdministrativa, p.modalidadEntrega,
            p.fechaEntregaAcordada, p.fechaEntregaReal, p.cotizacionAceptada, p.totalReferencial, p.total,
            p.anticipoExigible, p.saldoPendiente), e.direccionEntrega, e.motivoArchivo, e.fechaArchivo, e.versionCotizacionVigente,
            e.detalles.Select(d => new PrendaClienteDto(d.id, d.cantidad, d.talla, d.color, d.tela, d.empaque,
                d.ideaDiseno, d.observaciones, d.disenoAprobado, d.versionDisenoVigente)).ToList(),
            e.versionesCotizacion.Select(v => new CotizacionClienteDto(v.id, v.numeroVersion, v.fechaRegistro,
                v.decision, v.fechaDecision, v.total, v.detalleImportes)).ToList(),
            e.versionesDiseno.Select(v => new DisenoClienteDto(v.id, v.detalle, v.numeroVersion, v.descripcion,
                v.decision, v.fechaRegistro, v.fechaDecision)).ToList(),
            e.pagos.Select(g => new PagoClienteDto(g.id, g.monto, g.medio, g.referencia, g.fecha,
                g.estadoVerificacion, g.fechaVerificacion, g.tieneComprobante)).ToList());
    }
    public Task<string?> ReferenciaComprobanteAsync(int cliente, int pedido, int pago, CancellationToken ct) =>
        contexto.Pago.AsNoTracking().Where(g => g.id == pago && g.pedido == pedido && g.pedidoNavigation.cliente == cliente)
            .Select(g => g.comprobanteURL).SingleOrDefaultAsync(ct);
    public Task<bool> ArchivoReferenciadoAsync(string referencia, CancellationToken ct) =>
        contexto.Pago.AsNoTracking().AnyAsync(g => g.comprobanteURL == referencia, ct);
}
