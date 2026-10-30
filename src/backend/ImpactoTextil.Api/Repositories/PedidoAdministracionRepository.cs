using System.Data;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Repositories;

public sealed class PedidoAdministracionRepository(ImpactoTextilDbContext contexto) : IPedidoAdministracionRepository
{
    public Task<Usuario?> ObtenerAdministradorActivoAsync(int usuarioId, CancellationToken cancelacion) =>
        contexto.Usuario
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.id == usuarioId && u.activo && u.rol == "Administrador",
                cancelacion);

    public async Task<IReadOnlyList<ResumenPedidoAdminDto>> ListarPedidosAsync(
        string? estado,
        string condicionAdministrativa,
        CancellationToken cancelacion)
    {
        var consulta =
            from p in contexto.vPedido.AsNoTracking()
            join u in contexto.Usuario.AsNoTracking() on p.cliente equals u.id
            select new { Pedido = p, ClienteNombre = u.nombre, ClienteCorreo = u.correo };

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var estadoLimpio = estado.Trim();
            consulta = consulta.Where(x => x.Pedido.estado == estadoLimpio);
        }

        var condicionLimpia = string.IsNullOrWhiteSpace(condicionAdministrativa)
            ? "Activa"
            : condicionAdministrativa.Trim();

        if (!string.Equals(condicionLimpia, "Todas", StringComparison.Ordinal))
            consulta = consulta.Where(x => x.Pedido.condicionAdministrativa == condicionLimpia);

        var filas = await consulta
            .OrderByDescending(x => x.Pedido.fechaOrden)
            .ThenByDescending(x => x.Pedido.id)
            .ToListAsync(cancelacion);

        return filas
            .Select(x => MapearResumen(x.Pedido, x.ClienteNombre, x.ClienteCorreo))
            .ToList();
    }

    public async Task<ExpedientePedidoAdminDto?> ObtenerExpedienteAsync(
        int pedidoId,
        CancellationToken cancelacion)
    {
        var estrategia = contexto.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async ct =>
        {
            // Mantener una sola lectura coherente de cabecera, versiones y pagos.
            // El bloqueo inicial sigue el mismo orden que los servicios escritores.
            await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (contexto.Database.IsSqlServer())
            {
                var pedidos = await contexto.Database
                    .SqlQueryRaw<int>("SELECT [id] AS [Value] FROM [dbo].[Pedido] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE [id] = {0}", pedidoId)
                    .ToListAsync(ct);
                if (pedidos.Count == 0)
                {
                    await transaccion.CommitAsync(ct);
                    return null;
                }
            }

            var expediente = await LeerExpedienteAsync(pedidoId, ct);
            await transaccion.CommitAsync(ct);
            return expediente;
        }, cancelacion);
    }

    private async Task<ExpedientePedidoAdminDto?> LeerExpedienteAsync(
        int pedidoId,
        CancellationToken cancelacion)
    {
        var cabecera = await (
            from p in contexto.vPedido.AsNoTracking()
            join u in contexto.Usuario.AsNoTracking() on p.cliente equals u.id
            where p.id == pedidoId
            select new { Pedido = p, ClienteNombre = u.nombre, ClienteCorreo = u.correo })
            .FirstOrDefaultAsync(cancelacion);

        if (cabecera is null)
            return null;

        var detallesVista = await contexto.vDetallePedido
            .AsNoTracking()
            .Where(d => d.pedido == pedidoId)
            .OrderBy(d => d.id)
            .ToListAsync(cancelacion);

        var versionesCotizacionVista = await contexto.vVersionCotizacion
            .AsNoTracking()
            .Where(v => v.pedido == pedidoId)
            .OrderBy(v => v.numeroVersion)
            .ToListAsync(cancelacion);

        var idsCotizaciones = versionesCotizacionVista.Select(v => v.id).ToList();
        var lineasCotizacionVista = idsCotizaciones.Count == 0
            ? []
            : await contexto.vDetalleCotizacion
                .AsNoTracking()
                .Where(l => idsCotizaciones.Contains(l.versionCotizacion))
                .OrderBy(l => l.detalle)
                .ToListAsync(cancelacion);

        var idsDetalles = detallesVista.Select(d => d.id).ToList();
        var versionesDiseno = idsDetalles.Count == 0
            ? []
            : await contexto.VersionDiseno
                .AsNoTracking()
                .Where(v => idsDetalles.Contains(v.detalle))
                .OrderBy(v => v.detalle)
                .ThenBy(v => v.numeroVersion)
                .ToListAsync(cancelacion);

        var pagos = await contexto.Pago
            .AsNoTracking()
            .Where(g => g.pedido == pedidoId)
            .OrderBy(g => g.id)
            .ToListAsync(cancelacion);

        var constancias = await contexto.Constancia
            .AsNoTracking()
            .Where(c => c.pedido == pedidoId)
            .OrderBy(c => c.fecha)
            .ThenBy(c => c.registroOrigen)
            .ToListAsync(cancelacion);

        var comprobaciones = idsCotizaciones.Count == 0
            ? []
            : await contexto.ComprobacionSublimacion
                .AsNoTracking()
                .Include(c => c.versionDiseno)
                .Where(c => idsCotizaciones.Contains(c.versionCotizacion))
                .OrderBy(c => c.id)
                .ToListAsync(cancelacion);

        var pagosVerificadosAcumulados = pagos
            .Where(g => g.estadoVerificacion == "Verificado")
            .Sum(g => g.monto);

        var lineasPorCotizacion = lineasCotizacionVista
            .GroupBy(l => l.versionCotizacion)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<LineaCotizacionAdminDto>)g.Select(l => new LineaCotizacionAdminDto(
                    l.detalle,
                    l.cantidad,
                    l.precioUnitarioAcordado,
                    l.subtotal ?? (l.cantidad * l.precioUnitarioAcordado),
                    l.versionDiseno)).ToList());

        return new ExpedientePedidoAdminDto(
            pedido: MapearResumen(cabecera.Pedido, cabecera.ClienteNombre, cabecera.ClienteCorreo),
            direccionEntrega: cabecera.Pedido.direccionEntrega,
            motivoArchivo: cabecera.Pedido.motivoArchivo,
            fechaArchivo: cabecera.Pedido.fechaArchivo,
            responsableArchivo: cabecera.Pedido.responsableArchivo,
            versionCotizacionVigente: cabecera.Pedido.versionCotizacionVigente,
            pagosVerificadosAcumulados: pagosVerificadosAcumulados,
            detalles: detallesVista.Select(d => new DetallePedidoAdminDto(
                d.id,
                d.producto,
                d.cantidad,
                d.talla,
                d.color,
                d.tela,
                d.empaque,
                d.observaciones,
                d.ideaDiseno,
                !string.IsNullOrWhiteSpace(d.archivoDisenoURL),
                d.precioBaseReferencia,
                d.versionDisenoVigente,
                d.disenoAprobado ?? false,
                d.fechaAprobacionDiseno,
                d.responsableAprobacionDiseno,
                d.precioUnitarioAcordado,
                d.subtotal)).ToList(),
            versionesCotizacion: versionesCotizacionVista.Select(v => new VersionCotizacionAdminDto(
                v.id,
                v.numeroVersion,
                v.fechaRegistro,
                v.responsableRegistro,
                v.decision,
                v.fechaDecision,
                v.responsableDecision,
                v.referenciaDecision,
                v.total ?? 0m,
                lineasPorCotizacion.GetValueOrDefault(v.id, []))).ToList(),
            versionesDiseno: versionesDiseno.Select(v => new VersionDisenoAdminDto(
                v.id,
                v.detalle,
                v.numeroVersion,
                !string.IsNullOrWhiteSpace(v.archivoURL),
                v.descripcion,
                v.fechaRegistro,
                v.responsableRegistro,
                v.decision,
                v.fechaDecision,
                v.responsableDecision,
                v.referenciaDecision)).ToList(),
            pagos: pagos.Select(g => new PagoAdminDto(
                g.id,
                g.monto,
                g.fecha,
                g.medio,
                g.referencia,
                g.estadoVerificacion,
                !string.IsNullOrWhiteSpace(g.comprobanteURL),
                g.fechaVerificacion,
                g.responsableVerificacion)).ToList(),
            constancias: constancias.Select(c => new ConstanciaAdminDto(
                c.tipo ?? string.Empty,
                c.registroOrigen,
                c.decision ?? false,
                c.fecha ?? default,
                c.responsable ?? 0,
                c.referencia ?? string.Empty,
                c.versionCotizacion)).ToList(),
            comprobacionesSublimacion: comprobaciones.Select(c => new ComprobacionSublimacionAdminDto(
                c.id,
                c.versionCotizacion,
                c.fecha,
                c.responsable,
                c.montoVerificado,
                c.versionDiseno.Select(v => v.id).OrderBy(id => id).ToList())).ToList());
    }

    public async Task<ReferenciaArchivoDiseno> ObtenerReferenciaDisenoOriginalAsync(
        int pedidoId,
        int detalleId,
        CancellationToken cancelacion)
    {
        var dato = await (
            from d in contexto.DetallePedido.AsNoTracking()
            join p in contexto.Pedido.AsNoTracking() on d.pedido equals p.id
            where p.id == pedidoId && d.id == detalleId
            select new { p.codigo, d.id, d.archivoDisenoURL, d.ideaDiseno })
            .FirstOrDefaultAsync(cancelacion);

        return dato is null
            ? new ReferenciaArchivoDiseno(false, null, null, string.Empty)
            : new ReferenciaArchivoDiseno(
                true,
                dato.archivoDisenoURL,
                dato.ideaDiseno,
                $"diseno-original-{dato.codigo}-detalle-{dato.id}");
    }

    public async Task<ReferenciaArchivoDiseno> ObtenerReferenciaVersionDisenoAsync(
        int pedidoId,
        int detalleId,
        int versionDisenoId,
        CancellationToken cancelacion)
    {
        var dato = await (
            from v in contexto.VersionDiseno.AsNoTracking()
            join d in contexto.DetallePedido.AsNoTracking() on v.detalle equals d.id
            join p in contexto.Pedido.AsNoTracking() on d.pedido equals p.id
            where p.id == pedidoId && d.id == detalleId && v.id == versionDisenoId
            select new { p.codigo, d.id, v.numeroVersion, v.archivoURL, v.descripcion })
            .FirstOrDefaultAsync(cancelacion);

        return dato is null
            ? new ReferenciaArchivoDiseno(false, null, null, string.Empty)
            : new ReferenciaArchivoDiseno(
                true,
                dato.archivoURL,
                dato.descripcion,
                $"diseno-{dato.codigo}-detalle-{dato.id}-v{dato.numeroVersion}");
    }

    public async Task<ResultadoOperacionAdmin<T>> EjecutarEnPedidoBloqueadoAsync<T>(
        int pedidoId,
        Func<Pedido, Func<CancellationToken, Task>, CancellationToken, Task<ResultadoOperacionAdmin<T>>> operacion,
        CancellationToken cancelacion)
    {
        var estrategia = contexto.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async ct =>
        {
            await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                if (contexto.Database.IsSqlServer())
                {
                    var bloqueados = await contexto.Database
                        .SqlQueryRaw<int>("SELECT [id] AS [Value] FROM [dbo].[Pedido] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE [id] = {0}", pedidoId)
                        .ToListAsync(ct);

                    if (bloqueados.Count == 0)
                    {
                        await transaccion.RollbackAsync(ct);
                        return ResultadoOperacionAdmin<T>.Fallo(CodigoErrorAdmin.NoEncontrado, "El pedido solicitado no existe.");
                    }
                }

                contexto.ChangeTracker.Clear();

                var pedido = await contexto.Pedido
                    .Include(p => p.DetallePedido)
                        .ThenInclude(d => d.VersionDiseno)
                    .Include(p => p.VersionCotizacion)
                        .ThenInclude(v => v.DetalleCotizacion)
                    .Include(p => p.VersionCotizacion)
                        .ThenInclude(v => v.ComprobacionSublimacion)
                            .ThenInclude(c => c.versionDiseno)
                    .Include(p => p.Pago)
                    .Include(p => p.ConstanciaEntrega)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(p => p.id == pedidoId, ct);

                if (pedido is null)
                {
                    await transaccion.RollbackAsync(ct);
                    return ResultadoOperacionAdmin<T>.Fallo(CodigoErrorAdmin.NoEncontrado, "El pedido solicitado no existe.");
                }

                Task GuardarAsync(CancellationToken token) => contexto.SaveChangesAsync(token);

                var resultado = await operacion(pedido, GuardarAsync, ct);
                if (!resultado.EsExito)
                {
                    await transaccion.RollbackAsync(ct);
                    contexto.ChangeTracker.Clear();
                    return resultado;
                }

                await contexto.SaveChangesAsync(ct);
                await transaccion.CommitAsync(ct);
                return resultado;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaccion.RollbackAsync(ct);
                contexto.ChangeTracker.Clear();
                return ResultadoOperacionAdmin<T>.Fallo(
                    CodigoErrorAdmin.Concurrencia,
                    "El pedido o pago cambió mientras se procesaba la decisión. Recarga la información y vuelve a intentarlo.");
            }
            catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
            {
                await transaccion.RollbackAsync(ct);
                contexto.ChangeTracker.Clear();
                return ResultadoOperacionAdmin<T>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Ya existe un registro confirmado con la misma referencia, versión o clave de confirmación en este pedido.");
            }
        }, cancelacion);
    }

    private static ResumenPedidoAdminDto MapearResumen(vPedido vista, string clienteNombre, string clienteCorreo) =>
        new(
            id: vista.id,
            codigo: vista.codigo,
            clienteId: vista.cliente,
            clienteNombre: clienteNombre,
            clienteCorreo: clienteCorreo,
            fechaOrden: vista.fechaOrden,
            fechaEntregaAcordada: vista.fechaEntregaAcordada,
            fechaEntregaReal: vista.fechaEntregaReal,
            estado: vista.estado,
            modalidadEntrega: vista.modalidadEntrega,
            condicionAdministrativa: vista.condicionAdministrativa,
            totalReferencial: vista.totalReferencial ?? 0m,
            cotizacionAceptada: vista.cotizacionAceptada ?? false,
            total: vista.total,
            porcentajeAnticipo: vista.porcentajeAnticipo ?? ReglasAdministrativasPedido.PorcentajeAnticipoFijo,
            anticipoExigible: vista.anticipoExigible,
            saldoPendiente: vista.saldoPendiente);
}
