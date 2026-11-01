using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class ProduccionEntregaAdminService(
    IPedidoAdministracionRepository repositorio,
    TimeProvider reloj)
{
    public async Task<ResultadoOperacionAdmin<ResultadoAutorizacionSublimacionDto>> AutorizarSublimacionAsync(
        int administradorId,
        int pedidoId,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoAutorizacionSublimacionDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede autorizar la sublimación de un pedido.");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardarPasoAsync, ct) =>
        {
            var cotizacionVigente = PagosAdminService.ObtenerCotizacionAceptadaVigente(pedido);
            var totalAceptado = cotizacionVigente is null
                ? (decimal?)null
                : RevisionCotizacionAdminService.CalcularTotalVersion(pedido, cotizacionVigente);

            var pagosVerificados = pedido.Pago
                .Where(g => g.estadoVerificacion == "Verificado")
                .Sum(g => g.monto);

            var evaluacionDetalles = ConstruirEvaluacionDetalles(pedido);

            var error = ReglasAdministrativasPedido.EvaluarAutorizacionSublimacion(
                pedido.condicionAdministrativa,
                pedido.estado,
                cotizacionVigente is not null,
                totalAceptado,
                evaluacionDetalles,
                pagosVerificados);

            if (error is not null || cotizacionVigente is null || totalAceptado is null)
                return ResultadoOperacionAdmin<ResultadoAutorizacionSublimacionDto>.Fallo(
                    error?.codigo ?? CodigoErrorAdmin.ConflictoEstado,
                    error?.mensaje ?? "No se cumplen las condiciones vigentes para autorizar la sublimación.",
                    error?.campo);

            var versionesDisenoVigentes = pedido.DetallePedido
                .OrderBy(d => d.id)
                .Select(d => d.VersionDiseno.Single(v => v.id == d.versionDisenoVigente!.Value && v.decision == "Aprobada"))
                .ToList();

            var idsVigentes = versionesDisenoVigentes.Select(v => v.id).OrderBy(id => id).ToArray();
            var anticipoExigible = ReglasAdministrativasPedido.CalcularAnticipoExigible(totalAceptado.Value);

            var existenteExacta = cotizacionVigente.ComprobacionSublimacion
                .OrderByDescending(c => c.id)
                .FirstOrDefault(c =>
                    c.montoVerificado == pagosVerificados
                    && c.versionDiseno.Select(v => v.id).OrderBy(id => id).SequenceEqual(idsVigentes));

            if (existenteExacta is not null)
            {
                return ResultadoOperacionAdmin<ResultadoAutorizacionSublimacionDto>.Exito(new ResultadoAutorizacionSublimacionDto(
                    pedidoId: pedido.id,
                    comprobacionId: existenteExacta.id,
                    versionCotizacion: cotizacionVigente.id,
                    versionesDisenoComprobadas: idsVigentes,
                    totalAceptado: totalAceptado.Value,
                    anticipoExigible: anticipoExigible,
                    montoVerificado: existenteExacta.montoVerificado,
                    fecha: existenteExacta.fecha,
                    responsable: existenteExacta.responsable));
            }

            var ahora = RelojPedido.TruncarASegundosUtc(reloj.GetUtcNow());
            var comprobacion = new ComprobacionSublimacion
            {
                versionCotizacion = cotizacionVigente.id,
                fecha = ahora,
                responsable = administradorId,
                montoVerificado = pagosVerificados
            };

            foreach (var versionDiseno in versionesDisenoVigentes)
                comprobacion.versionDiseno.Add(versionDiseno);

            cotizacionVigente.ComprobacionSublimacion.Add(comprobacion);
            await guardarPasoAsync(ct);

            return ResultadoOperacionAdmin<ResultadoAutorizacionSublimacionDto>.Exito(new ResultadoAutorizacionSublimacionDto(
                pedidoId: pedido.id,
                comprobacionId: comprobacion.id,
                versionCotizacion: cotizacionVigente.id,
                versionesDisenoComprobadas: idsVigentes,
                totalAceptado: totalAceptado.Value,
                anticipoExigible: anticipoExigible,
                montoVerificado: pagosVerificados,
                fecha: ahora,
                responsable: administradorId));
        }, cancelacion);
    }

    public async Task<ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>> ActualizarEstadoAsync(
        int administradorId,
        int pedidoId,
        ActualizarEstadoProduccionDto solicitud,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede actualizar el avance de producción y entrega.");

        var nuevoEstado = solicitud.nuevoEstado?.Trim();
        if (nuevoEstado is not ("En producción" or "Listo para entrega" or "Entregado"))
            return ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "El estado solicitado debe ser En producción, Listo para entrega o Entregado.",
                "nuevoEstado");

        var referenciaEntrega = string.IsNullOrWhiteSpace(solicitud.referenciaConfirmacionEntrega)
            ? null
            : solicitud.referenciaConfirmacionEntrega.Trim();

        if (nuevoEstado == "Entregado" && (referenciaEntrega is null || referenciaEntrega.Length > 250))
            return ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Registra la referencia de confirmación del cliente (1 a 250 caracteres) para marcar el pedido como Entregado.",
                "referenciaConfirmacionEntrega");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardarPasoAsync, ct) =>
        {
            var cotizacionVigente = PagosAdminService.ObtenerCotizacionAceptadaVigente(pedido);
            var totalAceptado = cotizacionVigente is null
                ? (decimal?)null
                : RevisionCotizacionAdminService.CalcularTotalVersion(pedido, cotizacionVigente);

            var pagosVerificados = pedido.Pago
                .Where(g => g.estadoVerificacion == "Verificado")
                .Sum(g => g.monto);

            var evaluacionDetalles = ConstruirEvaluacionDetalles(pedido);
            var estadoAnterior = pedido.estado;
            int? constanciaEntregaId = null;

            switch (nuevoEstado)
            {
                case "En producción":
                {
                    var error = ReglasAdministrativasPedido.EvaluarInicioProduccion(
                        pedido.condicionAdministrativa,
                        pedido.estado,
                        cotizacionVigente is not null,
                        totalAceptado,
                        pedido.fechaEntregaAcordada,
                        evaluacionDetalles,
                        pagosVerificados);

                    if (error is not null)
                        return ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>.Fallo(error.codigo, error.mensaje, error.campo);

                    pedido.estado = "En producción";
                    break;
                }

                case "Listo para entrega":
                {
                    var tieneComprobacionVigente = TieneComprobacionSublimacionVigente(pedido, cotizacionVigente);
                    var error = ReglasAdministrativasPedido.EvaluarPasoAListoParaEntrega(
                        pedido.condicionAdministrativa,
                        pedido.estado,
                        cotizacionVigente is not null,
                        totalAceptado,
                        evaluacionDetalles,
                        pagosVerificados,
                        tieneComprobacionVigente);

                    if (error is not null)
                        return ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>.Fallo(error.codigo, error.mensaje, error.campo);

                    pedido.estado = "Listo para entrega";
                    break;
                }

                case "Entregado":
                {
                    var error = ReglasAdministrativasPedido.EvaluarEntrega(
                        pedido.condicionAdministrativa,
                        pedido.estado,
                        cotizacionVigente is not null,
                        totalAceptado,
                        pagosVerificados,
                        referenciaEntrega);

                    if (error is not null)
                        return ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>.Fallo(error.codigo, error.mensaje, error.campo);

                    var ahora = RelojPedido.TruncarASegundosUtc(reloj.GetUtcNow());
                    var constancia = new ConstanciaEntrega
                    {
                        pedido = pedido.id,
                        decision = true,
                        fecha = ahora,
                        responsable = administradorId,
                        referencia = referenciaEntrega!
                    };

                    pedido.ConstanciaEntrega.Add(constancia);
                    pedido.estado = "Entregado";
                    pedido.fechaEntregaReal = ahora;
                    await guardarPasoAsync(ct);
                    constanciaEntregaId = constancia.id;
                    break;
                }
            }

            var totalDefinitivo = totalAceptado!.Value;
            var anticipo = ReglasAdministrativasPedido.CalcularAnticipoExigible(totalDefinitivo);
            var saldo = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalDefinitivo, pagosVerificados);

            return ResultadoOperacionAdmin<ResultadoEstadoProduccionDto>.Exito(new ResultadoEstadoProduccionDto(
                pedidoId: pedido.id,
                codigo: pedido.codigo,
                estadoAnterior: estadoAnterior,
                estadoActual: pedido.estado,
                fechaEntregaReal: pedido.fechaEntregaReal,
                constanciaEntregaId: constanciaEntregaId,
                totalAceptado: totalDefinitivo,
                anticipoExigible: anticipo,
                saldoPendiente: saldo));
        }, cancelacion);
    }

    private static List<EstadoDetalleEvaluacion> ConstruirEvaluacionDetalles(Pedido pedido) =>
        pedido.DetallePedido
            .OrderBy(d => d.id)
            .Select(d =>
            {
                var vigenteValida = d.versionDisenoVigente is not null
                    && d.VersionDiseno.Any(v => v.id == d.versionDisenoVigente.Value && v.detalle == d.id && v.decision == "Aprobada");
                return new EstadoDetalleEvaluacion(d.id, d.versionDisenoVigente, vigenteValida);
            })
            .ToList();

    private static bool TieneComprobacionSublimacionVigente(Pedido pedido, VersionCotizacion? cotizacionVigente)
    {
        if (cotizacionVigente is null || pedido.DetallePedido.Count == 0 || pedido.DetallePedido.Any(d => d.versionDisenoVigente is null))
            return false;

        var idsDisenosVigentes = pedido.DetallePedido
            .Select(d => d.versionDisenoVigente!.Value)
            .OrderBy(id => id)
            .ToArray();

        return cotizacionVigente.ComprobacionSublimacion.Any(c =>
            c.versionDiseno.Select(v => v.id).OrderBy(id => id).SequenceEqual(idsDisenosVigentes));
    }
}
