using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class PagosAdminService(
    IPedidoAdministracionRepository repositorio,
    TimeProvider reloj)
{
    public async Task<ResultadoOperacionAdmin<ResultadoPagoAdminDto>> ResolverPagoPendienteAsync(
        int administradorId,
        int pedidoId,
        int pagoId,
        ResolverPagoPendienteDto solicitud,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede verificar o rechazar comprobantes de pago.");

        var decisionLimpia = solicitud.decision?.Trim();
        if (decisionLimpia is not ("Verificado" or "Rechazado"))
            return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "La decisión sobre el pago debe ser Verificado o Rechazado.",
                "decision");

        var referenciaLimpia = string.IsNullOrWhiteSpace(solicitud.referencia) ? null : solicitud.referencia.Trim();
        if (decisionLimpia == "Verificado" && (referenciaLimpia is null || referenciaLimpia.Length > 100))
            return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Al verificar un pago debes confirmar su referencia (1 a 100 caracteres).",
                "referencia");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, (pedido, _, _) =>
        {
            if (pedido.condicionAdministrativa == "Archivada")
                return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Una solicitud archivada no admite verificación ni registro de pagos."));

            var cotizacionVigente = ObtenerCotizacionAceptadaVigente(pedido);
            if (cotizacionVigente is null)
                return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "El pedido debe contar con una cotización aceptada vigente antes de resolver pagos.",
                    "versionCotizacionVigente"));

            var pago = pedido.Pago.FirstOrDefault(g => g.id == pagoId);
            if (pago is null)
                return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.NoEncontrado,
                    "El registro de pago solicitado no pertenece a este pedido.",
                    "pagoId"));

            var totalAceptado = RevisionCotizacionAdminService.CalcularTotalVersion(pedido, cotizacionVigente);
            var anticipoExigible = ReglasAdministrativasPedido.CalcularAnticipoExigible(totalAceptado);
            var verificadosActuales = pedido.Pago
                .Where(g => g.estadoVerificacion == "Verificado")
                .Sum(g => g.monto);

            // TR_Pago_decisionUnica impide volver a actualizar un pago resuelto; el mismo envío devuelve su resultado idempotente.
            if (pago.estadoVerificacion != "Pendiente")
            {
                var mismaDecision = string.Equals(pago.estadoVerificacion, decisionLimpia, StringComparison.Ordinal)
                    && (decisionLimpia == "Rechazado"
                        ? referenciaLimpia is null || string.Equals(pago.referencia, referenciaLimpia, StringComparison.OrdinalIgnoreCase)
                        : string.Equals(pago.referencia, referenciaLimpia, StringComparison.OrdinalIgnoreCase)
                            && (solicitud.fechaCobroConfirmada is null || pago.fecha == RelojPedido.TruncarASegundosUtc(solicitud.fechaCobroConfirmada.Value)));

                if (mismaDecision)
                {
                    var saldoIdempotente = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalAceptado, verificadosActuales);
                    return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Exito(CrearResultado(
                        pedido.id,
                        pago,
                        totalAceptado,
                        verificadosActuales,
                        anticipoExigible,
                        saldoIdempotente,
                        esCreacionNueva: false)));
                }

                return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Este pago ya fue resuelto previamente y su decisión no puede sobrescribirse."));
            }

            var ahora = RelojPedido.TruncarASegundosUtc(reloj.GetUtcNow());

            if (decisionLimpia == "Verificado")
            {
                if (string.IsNullOrWhiteSpace(pago.comprobanteURL))
                    return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                        CodigoErrorAdmin.ConflictoEstado,
                        "No se puede verificar un pago pendiente que carece de evidencia adjunta.",
                        "comprobanteURL"));

                if (!ReglasAdministrativasPedido.EsImporteUnitarioValido(pago.monto))
                    return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                        CodigoErrorAdmin.Validacion,
                        "El monto del pago debe ser positivo y tener como máximo dos decimales.",
                        "monto"));

                var saldoDisponible = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalAceptado, verificadosActuales);
                if (pago.monto > saldoDisponible)
                    return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                        CodigoErrorAdmin.ConflictoEstado,
                        $"El monto del comprobante (C${pago.monto:F2}) supera el saldo pendiente actual (C${saldoDisponible:F2}).",
                        "monto"));

                if (ExisteReferenciaVerificadaDuplicada(pedido, pago.medio, referenciaLimpia!, pago.id))
                    return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                        CodigoErrorAdmin.ConflictoEstado,
                        "Ya existe otro pago verificado en este pedido con el mismo medio y referencia.",
                        "referencia"));

                if (solicitud.fechaCobroConfirmada is not null)
                    pago.fecha = RelojPedido.TruncarASegundosUtc(solicitud.fechaCobroConfirmada.Value);

                pago.referencia = referenciaLimpia;
                pago.estadoVerificacion = "Verificado";
                pago.fechaVerificacion = ahora;
                pago.responsableVerificacion = administradorId;

                var nuevosVerificados = verificadosActuales + pago.monto;
                var nuevoSaldo = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalAceptado, nuevosVerificados);
                return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Exito(CrearResultado(
                    pedido.id,
                    pago,
                    totalAceptado,
                    nuevosVerificados,
                    anticipoExigible,
                    nuevoSaldo,
                    esCreacionNueva: false)));
            }

            if (referenciaLimpia is not null)
                pago.referencia = referenciaLimpia;

            pago.estadoVerificacion = "Rechazado";
            pago.fechaVerificacion = ahora;
            pago.responsableVerificacion = administradorId;

            var saldoSinDescuento = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalAceptado, verificadosActuales);
            return Task.FromResult(ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Exito(CrearResultado(
                pedido.id,
                pago,
                totalAceptado,
                verificadosActuales,
                anticipoExigible,
                saldoSinDescuento,
                esCreacionNueva: false)));
        }, cancelacion);
    }

    public async Task<ResultadoOperacionAdmin<ResultadoPagoAdminDto>> RegistrarCobroDirectoAsync(
        int administradorId,
        int pedidoId,
        RegistrarCobroDirectoDto solicitud,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede registrar cobros directos verificados.");

        var medioLimpio = solicitud.medio?.Trim();
        var referenciaLimpia = solicitud.referencia?.Trim();
        if (!ReglasAdministrativasPedido.EsImporteUnitarioValido(solicitud.monto)
            || solicitud.fecha == default
            || medioLimpio is null || !ReglasAdministrativasPedido.MediosPagoValidos.Contains(medioLimpio)
            || string.IsNullOrWhiteSpace(referenciaLimpia) || referenciaLimpia.Length > 100
            || solicitud.confirmacion == Guid.Empty)
        {
            return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Completa monto positivo, fecha de recepción, medio permitido, constancia/referencia y clave de confirmación.");
        }

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardarPasoAsync, ct) =>
        {
            if (pedido.condicionAdministrativa == "Archivada")
                return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Una solicitud archivada no admite cobros.");

            var cotizacionVigente = ObtenerCotizacionAceptadaVigente(pedido);
            if (cotizacionVigente is null)
                return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "El pedido debe tener una cotización aceptada antes de acreditar un cobro directo.",
                    "versionCotizacionVigente");

            var totalAceptado = RevisionCotizacionAdminService.CalcularTotalVersion(pedido, cotizacionVigente);
            var anticipoExigible = ReglasAdministrativasPedido.CalcularAnticipoExigible(totalAceptado);
            var verificadosActuales = pedido.Pago
                .Where(g => g.estadoVerificacion == "Verificado")
                .Sum(g => g.monto);

            var existentePorConfirmacion = pedido.Pago.FirstOrDefault(g => g.confirmacion == solicitud.confirmacion);
            if (existentePorConfirmacion is not null)
            {
                var mismosDatos = existentePorConfirmacion.estadoVerificacion == "Verificado"
                    && existentePorConfirmacion.monto == solicitud.monto
                    && existentePorConfirmacion.fecha == RelojPedido.TruncarASegundosUtc(solicitud.fecha)
                    && string.Equals(existentePorConfirmacion.medio, medioLimpio, StringComparison.Ordinal)
                    && string.Equals(existentePorConfirmacion.referencia, referenciaLimpia, StringComparison.OrdinalIgnoreCase);

                if (mismosDatos)
                {
                    var saldoActual = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalAceptado, verificadosActuales);
                    return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Exito(CrearResultado(
                        pedido.id,
                        existentePorConfirmacion,
                        totalAceptado,
                        verificadosActuales,
                        anticipoExigible,
                        saldoActual,
                        esCreacionNueva: false));
                }

                return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "La clave de confirmación ya fue utilizada con datos distintos en este pedido.",
                    "confirmacion");
            }

            var saldoDisponible = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalAceptado, verificadosActuales);
            if (solicitud.monto > saldoDisponible)
                return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    $"El monto del cobro (C${solicitud.monto:F2}) supera el saldo pendiente actual (C${saldoDisponible:F2}).",
                    "monto");

            if (ExisteReferenciaVerificadaDuplicada(pedido, medioLimpio, referenciaLimpia, pagoExcluidoId: null))
                return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Ya existe un pago verificado en este pedido con el mismo medio y referencia.",
                    "referencia");

            var ahora = RelojPedido.TruncarASegundosUtc(reloj.GetUtcNow());
            var nuevoPago = new Pago
            {
                pedido = pedido.id,
                monto = solicitud.monto,
                fecha = RelojPedido.TruncarASegundosUtc(solicitud.fecha),
                medio = medioLimpio,
                referencia = referenciaLimpia,
                estadoVerificacion = "Verificado",
                comprobanteURL = null,
                fechaVerificacion = ahora,
                responsableVerificacion = administradorId,
                confirmacion = solicitud.confirmacion
            };

            pedido.Pago.Add(nuevoPago);
            await guardarPasoAsync(ct);

            var nuevosVerificados = verificadosActuales + nuevoPago.monto;
            var nuevoSaldo = ReglasAdministrativasPedido.CalcularSaldoPendiente(totalAceptado, nuevosVerificados);
            return ResultadoOperacionAdmin<ResultadoPagoAdminDto>.Exito(CrearResultado(
                pedido.id,
                nuevoPago,
                totalAceptado,
                nuevosVerificados,
                anticipoExigible,
                nuevoSaldo,
                esCreacionNueva: true));
        }, cancelacion);
    }

    internal static VersionCotizacion? ObtenerCotizacionAceptadaVigente(Pedido pedido) =>
        pedido.versionCotizacionVigente is null
            ? null
            : pedido.VersionCotizacion.FirstOrDefault(
                v => v.id == pedido.versionCotizacionVigente.Value && v.decision == "Aceptada");

    private static bool ExisteReferenciaVerificadaDuplicada(
        Pedido pedido,
        string medio,
        string referencia,
        int? pagoExcluidoId) =>
        pedido.Pago.Any(g =>
            g.id != pagoExcluidoId
            && g.estadoVerificacion == "Verificado"
            && string.Equals(g.medio, medio, StringComparison.Ordinal)
            && string.Equals(g.referencia, referencia, StringComparison.OrdinalIgnoreCase));

    private static ResultadoPagoAdminDto CrearResultado(
        int pedidoId,
        Pago pago,
        decimal totalAceptado,
        decimal pagosVerificadosAcumulados,
        decimal anticipoExigible,
        decimal saldoPendiente,
        bool esCreacionNueva) =>
        new(
            pedidoId: pedidoId,
            pagoId: pago.id,
            estadoVerificacion: pago.estadoVerificacion,
            monto: pago.monto,
            medio: pago.medio,
            referencia: pago.referencia,
            fechaVerificacion: pago.fechaVerificacion ?? default,
            responsableVerificacion: pago.responsableVerificacion ?? 0,
            totalAceptado: totalAceptado,
            pagosVerificadosAcumulados: pagosVerificadosAcumulados,
            anticipoExigible: anticipoExigible,
            saldoPendiente: saldoPendiente,
            esCreacionNueva: esCreacionNueva);
}
