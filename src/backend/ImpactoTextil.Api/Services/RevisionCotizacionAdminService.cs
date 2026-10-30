using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class RevisionCotizacionAdminService(
    IPedidoAdministracionRepository repositorio,
    TimeProvider reloj)
{
    public async Task<ResultadoOperacionAdmin<IReadOnlyList<ResumenPedidoAdminDto>>> ListarPedidosAsync(
        int administradorId,
        FiltroPedidosAdminDto filtro,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<IReadOnlyList<ResumenPedidoAdminDto>>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede consultar el panel de pedidos.");

        var condicion = string.IsNullOrWhiteSpace(filtro.condicionAdministrativa)
            ? "Activa"
            : filtro.condicionAdministrativa.Trim();

        var pedidos = await repositorio.ListarPedidosAsync(filtro.estado, condicion, cancelacion);
        return ResultadoOperacionAdmin<IReadOnlyList<ResumenPedidoAdminDto>>.Exito(pedidos);
    }

    public async Task<ResultadoOperacionAdmin<ExpedientePedidoAdminDto>> ObtenerExpedienteAsync(
        int administradorId,
        int pedidoId,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ExpedientePedidoAdminDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede revisar expedientes de pedidos.");

        var expediente = await repositorio.ObtenerExpedienteAsync(pedidoId, cancelacion);
        return expediente is null
            ? ResultadoOperacionAdmin<ExpedientePedidoAdminDto>.Fallo(
                CodigoErrorAdmin.NoEncontrado,
                "El pedido solicitado no existe.")
            : ResultadoOperacionAdmin<ExpedientePedidoAdminDto>.Exito(expediente);
    }

    public async Task<ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>> IncorporarVersionDisenoAsync(
        int administradorId,
        int pedidoId,
        int detalleId,
        string? archivoRelativo,
        string? descripcion,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede registrar revisiones de diseño.");

        var archivoLimpio = string.IsNullOrWhiteSpace(archivoRelativo) ? null : archivoRelativo.Trim();
        var descripcionLimpia = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

        if (archivoLimpio is null && descripcionLimpia is null)
            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Debes aportar un archivo de diseño validado o una descripción de la versión.",
                "descripcion");

        if (descripcionLimpia is not null && descripcionLimpia.Length > 1000)
            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "La descripción de la versión de diseño no puede exceder 1000 caracteres.",
                "descripcion");

        if (archivoLimpio is not null && archivoLimpio.Length > 2048)
            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "La referencia interna del archivo excede la longitud permitida.",
                "archivoURL");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardarPasoAsync, ct) =>
        {
            if (pedido.condicionAdministrativa == "Archivada")
                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Una solicitud archivada no admite nuevas versiones de diseño.");

            if (pedido.estado is "Listo para entrega" or "Entregado")
                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "No se admiten revisiones de diseño cuando la producción ya concluyó.");

            var detalle = pedido.DetallePedido.FirstOrDefault(d => d.id == detalleId);
            if (detalle is null)
                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.NoEncontrado,
                    "El detalle indicado no pertenece a este pedido.",
                    "detalle");

            var siguienteNumero = (detalle.VersionDiseno.MaxBy(v => v.numeroVersion)?.numeroVersion ?? 0) + 1;
            var nuevaVersion = new VersionDiseno
            {
                detalle = detalle.id,
                numeroVersion = siguienteNumero,
                archivoURL = archivoLimpio,
                descripcion = descripcionLimpia,
                fechaRegistro = RelojPedido.TruncarASegundosUtc(reloj.GetUtcNow()),
                responsableRegistro = administradorId,
                decision = "Pendiente"
            };

            detalle.VersionDiseno.Add(nuevaVersion);
            await guardarPasoAsync(ct);

            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Exito(new ResultadoVersionDisenoAdminDto(
                pedidoId: pedido.id,
                detalleId: detalle.id,
                versionDisenoId: nuevaVersion.id,
                numeroVersion: nuevaVersion.numeroVersion,
                decision: nuevaVersion.decision,
                versionDisenoVigente: detalle.versionDisenoVigente,
                disenoAprobado: detalle.versionDisenoVigente is not null));
        }, cancelacion);
    }

    public async Task<ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>> DecidirVersionDisenoAsync(
        int administradorId,
        int pedidoId,
        int detalleId,
        int versionDisenoId,
        DecidirVersionDisenoDto solicitud,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede registrar decisiones de aprobación de diseño.");

        var decisionLimpia = solicitud.decision?.Trim();
        var referenciaLimpia = solicitud.referenciaDecision?.Trim();
        if (decisionLimpia is not ("Aprobada" or "Rechazada") || string.IsNullOrWhiteSpace(referenciaLimpia) || referenciaLimpia.Length > 250)
            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Indica una decisión válida (Aprobada o Rechazada) y su referencia documentada de hasta 250 caracteres.");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardarPasoAsync, ct) =>
        {
            if (pedido.condicionAdministrativa == "Archivada")
                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Una solicitud archivada no admite decisiones sobre versiones de diseño.");

            if (pedido.estado is "Listo para entrega" or "Entregado")
                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "No se pueden modificar aprobaciones de diseño cuando el pedido ya terminó producción.");

            var detalle = pedido.DetallePedido.FirstOrDefault(d => d.id == detalleId);
            if (detalle is null)
                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.NoEncontrado,
                    "El detalle indicado no pertenece a este pedido.",
                    "detalle");

            var version = detalle.VersionDiseno.FirstOrDefault(v => v.id == versionDisenoId);
            if (version is null)
                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.NoEncontrado,
                    "La versión de diseño indicada no pertenece a esta prenda.",
                    "versionDiseno");

            if (version.decision != "Pendiente")
            {
                if (string.Equals(version.decision, decisionLimpia, StringComparison.Ordinal))
                {
                    return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Exito(new ResultadoVersionDisenoAdminDto(
                        pedidoId: pedido.id,
                        detalleId: detalle.id,
                        versionDisenoId: version.id,
                        numeroVersion: version.numeroVersion,
                        decision: version.decision,
                        versionDisenoVigente: detalle.versionDisenoVigente,
                        disenoAprobado: detalle.versionDisenoVigente is not null));
                }

                return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Esta versión de diseño ya fue resuelta anteriormente; registra una nueva versión para otra revisión.");
            }

            version.decision = decisionLimpia;
            version.fechaDecision = RelojPedido.NoAnteriorA(reloj.GetUtcNow(), version.fechaRegistro);
            version.responsableDecision = administradorId;
            version.referenciaDecision = referenciaLimpia;

            // TR_Detalle_versionPropia exige que la versión figure Aprobada antes de apuntar versionDisenoVigente.
            await guardarPasoAsync(ct);

            if (decisionLimpia == "Aprobada")
                detalle.versionDisenoVigente = version.id;

            return ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>.Exito(new ResultadoVersionDisenoAdminDto(
                pedidoId: pedido.id,
                detalleId: detalle.id,
                versionDisenoId: version.id,
                numeroVersion: version.numeroVersion,
                decision: version.decision,
                versionDisenoVigente: detalle.versionDisenoVigente,
                disenoAprobado: detalle.versionDisenoVigente is not null));
        }, cancelacion);
    }

    public async Task<ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>> ProponerCotizacionAsync(
        int administradorId,
        int pedidoId,
        ProponerCotizacionDto solicitud,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede registrar propuestas de cotización.");

        if (solicitud.lineas is null || solicitud.lineas.Count == 0)
            return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Debes cotizar todos los detalles del pedido.",
                "lineas");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardarPasoAsync, ct) =>
        {
            if (pedido.condicionAdministrativa == "Archivada")
                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Una solicitud archivada no admite nuevas cotizaciones.");

            if (pedido.estado is "Listo para entrega" or "Entregado")
                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "No se admiten nuevas cotizaciones cuando el pedido ya está listo para entrega o entregado.");

            if (pedido.DetallePedido.Count == 0)
                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "El pedido no posee detalles para cotizar.");

            var detallesPorId = pedido.DetallePedido.ToDictionary(d => d.id);
            if (solicitud.lineas.Count != detallesPorId.Count || solicitud.lineas.Select(l => l.detalle).Distinct().Count() != detallesPorId.Count)
                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.Validacion,
                    "La cotización debe incluir exactamente una línea de precio por cada detalle del pedido.",
                    "lineas");

            var lineasEntidad = new List<DetalleCotizacion>(solicitud.lineas.Count);
            decimal totalPropuesto = 0m;

            foreach (var linea in solicitud.lineas)
            {
                if (!detallesPorId.TryGetValue(linea.detalle, out var detallePedido))
                    return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                        CodigoErrorAdmin.Validacion,
                        $"El detalle {linea.detalle} no pertenece a este pedido.",
                        "lineas");

                if (!ReglasAdministrativasPedido.EsImporteUnitarioValido(linea.precioUnitarioAcordado))
                    return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                        CodigoErrorAdmin.Validacion,
                        "Cada precio unitario debe ser positivo y tener como máximo dos decimales.",
                        "precioUnitarioAcordado");

                if (linea.versionDiseno is not null && detallePedido.VersionDiseno.All(v => v.id != linea.versionDiseno.Value))
                    return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                        CodigoErrorAdmin.Validacion,
                        $"La versión de diseño {linea.versionDiseno.Value} no pertenece al detalle {detallePedido.id}.",
                        "versionDiseno");

                totalPropuesto += ReglasAdministrativasPedido.CalcularSubtotal(detallePedido.cantidad, linea.precioUnitarioAcordado);
                lineasEntidad.Add(new DetalleCotizacion
                {
                    detalle = detallePedido.id,
                    precioUnitarioAcordado = linea.precioUnitarioAcordado,
                    versionDiseno = linea.versionDiseno
                });
            }

            var siguienteNumero = (pedido.VersionCotizacion.MaxBy(v => v.numeroVersion)?.numeroVersion ?? 0) + 1;
            var nuevaCotizacion = new VersionCotizacion
            {
                pedido = pedido.id,
                numeroVersion = siguienteNumero,
                fechaRegistro = RelojPedido.TruncarASegundosUtc(reloj.GetUtcNow()),
                responsableRegistro = administradorId,
                decision = "Propuesta",
                DetalleCotizacion = lineasEntidad
            };

            pedido.VersionCotizacion.Add(nuevaCotizacion);
            await guardarPasoAsync(ct);

            var estadoFinancieroVigente = CalcularResumenVigente(pedido);
            return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Exito(new ResultadoCotizacionAdminDto(
                pedidoId: pedido.id,
                versionCotizacionId: nuevaCotizacion.id,
                numeroVersion: nuevaCotizacion.numeroVersion,
                decision: nuevaCotizacion.decision,
                totalPropuesto: totalPropuesto,
                versionCotizacionVigente: pedido.versionCotizacionVigente,
                totalVigente: estadoFinancieroVigente.totalVigente,
                anticipoExigible: estadoFinancieroVigente.anticipoExigible,
                saldoPendiente: estadoFinancieroVigente.saldoPendiente,
                fechaEntregaAcordada: pedido.fechaEntregaAcordada));
        }, cancelacion);
    }

    public async Task<ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>> DecidirCotizacionAsync(
        int administradorId,
        int pedidoId,
        int versionCotizacionId,
        DecidirCotizacionDto solicitud,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede registrar la aceptación o el rechazo de una cotización.");

        var decisionLimpia = solicitud.decision?.Trim();
        var referenciaLimpia = solicitud.referenciaDecision?.Trim();
        if (decisionLimpia is not ("Aceptada" or "Rechazada") || string.IsNullOrWhiteSpace(referenciaLimpia) || referenciaLimpia.Length > 250)
            return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Indica una decisión válida (Aceptada o Rechazada) y su constancia de hasta 250 caracteres.");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardarPasoAsync, ct) =>
        {
            if (pedido.condicionAdministrativa == "Archivada")
                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Una solicitud archivada no admite decisiones de cotización.");

            if (pedido.estado is "Listo para entrega" or "Entregado")
                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "No se pueden modificar acuerdos de cotización cuando el pedido ya está listo para entrega o entregado.");

            var version = pedido.VersionCotizacion.FirstOrDefault(v => v.id == versionCotizacionId);
            if (version is null)
                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.NoEncontrado,
                    "La versión de cotización indicada no pertenece a este pedido.",
                    "versionCotizacion");

            var totalVersion = CalcularTotalVersion(pedido, version);

            if (version.decision != "Propuesta")
            {
                if (string.Equals(version.decision, decisionLimpia, StringComparison.Ordinal))
                {
                    var vigenteIdempotente = CalcularResumenVigente(pedido);
                    return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Exito(new ResultadoCotizacionAdminDto(
                        pedidoId: pedido.id,
                        versionCotizacionId: version.id,
                        numeroVersion: version.numeroVersion,
                        decision: version.decision,
                        totalPropuesto: totalVersion,
                        versionCotizacionVigente: pedido.versionCotizacionVigente,
                        totalVigente: vigenteIdempotente.totalVigente,
                        anticipoExigible: vigenteIdempotente.anticipoExigible,
                        saldoPendiente: vigenteIdempotente.saldoPendiente,
                        fechaEntregaAcordada: pedido.fechaEntregaAcordada));
                }

                return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                    CodigoErrorAdmin.ConflictoEstado,
                    "Esta versión de cotización ya recibió una decisión definitiva; crea otra versión si hubo una nueva negociación.");
            }

            if (decisionLimpia == "Aceptada")
            {
                if (version.DetalleCotizacion.Count != pedido.DetallePedido.Count)
                    return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                        CodigoErrorAdmin.ConflictoEstado,
                        "La versión propuesta no cubre todos los detalles del pedido.");

                var fechaAcordadaFinal = solicitud.fechaEntregaAcordada ?? pedido.fechaEntregaAcordada;
                if (fechaAcordadaFinal is null)
                    return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                        CodigoErrorAdmin.Validacion,
                        "Al confirmar la aceptación de la cotización es obligatorio registrar la fecha de entrega acordada.",
                        "fechaEntregaAcordada");

                var pagosVerificados = pedido.Pago
                    .Where(g => g.estadoVerificacion == "Verificado")
                    .Sum(g => g.monto);

                if (totalVersion < pagosVerificados)
                    return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Fallo(
                        CodigoErrorAdmin.ConflictoEstado,
                        $"No se puede aceptar un nuevo total (C${totalVersion:F2}) inferior a los cobros ya verificados (C${pagosVerificados:F2}).",
                        "total");

                version.decision = "Aceptada";
                version.fechaDecision = RelojPedido.NoAnteriorA(reloj.GetUtcNow(), version.fechaRegistro);
                version.responsableDecision = administradorId;
                version.referenciaDecision = referenciaLimpia;

                // TR_Pedido_versionPropia exige que la cotización esté Aceptada antes de asignarla como vigente.
                await guardarPasoAsync(ct);

                pedido.versionCotizacionVigente = version.id;
                pedido.fechaEntregaAcordada = fechaAcordadaFinal;
            }
            else
            {
                version.decision = "Rechazada";
                version.fechaDecision = RelojPedido.NoAnteriorA(reloj.GetUtcNow(), version.fechaRegistro);
                version.responsableDecision = administradorId;
                version.referenciaDecision = referenciaLimpia;
            }

            var estadoFinanciero = CalcularResumenVigente(pedido);
            return ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>.Exito(new ResultadoCotizacionAdminDto(
                pedidoId: pedido.id,
                versionCotizacionId: version.id,
                numeroVersion: version.numeroVersion,
                decision: version.decision,
                totalPropuesto: totalVersion,
                versionCotizacionVigente: pedido.versionCotizacionVigente,
                totalVigente: estadoFinanciero.totalVigente,
                anticipoExigible: estadoFinanciero.anticipoExigible,
                saldoPendiente: estadoFinanciero.saldoPendiente,
                fechaEntregaAcordada: pedido.fechaEntregaAcordada));
        }, cancelacion);
    }

    public async Task<ResultadoOperacionAdmin<ResultadoArchivoPedidoDto>> ArchivarSolicitudAsync(
        int administradorId,
        int pedidoId,
        ArchivarPedidoDto solicitud,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ResultadoArchivoPedidoDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo puede archivar administrativamente una solicitud.");

        var motivoLimpio = solicitud.motivoArchivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivoLimpio) || motivoLimpio.Length > 250)
            return ResultadoOperacionAdmin<ResultadoArchivoPedidoDto>.Fallo(
                CodigoErrorAdmin.Validacion,
                "Registra el motivo documentado del archivo (1 a 250 caracteres).",
                "motivoArchivo");

        return await repositorio.EjecutarEnPedidoBloqueadoAsync(pedidoId, (pedido, _, _) =>
        {
            var tieneCotizacionAceptada = pedido.versionCotizacionVigente is not null
                || pedido.VersionCotizacion.Any(v => v.decision == "Aceptada");

            var tienePagosActivos = pedido.Pago.Any(g => g.estadoVerificacion is "Verificado" or "Pendiente");

            var errorRegla = ReglasAdministrativasPedido.EvaluarArchivoAdministrativo(
                pedido.condicionAdministrativa,
                pedido.estado,
                tieneCotizacionAceptada,
                tienePagosActivos);

            if (errorRegla is not null)
                return Task.FromResult(ResultadoOperacionAdmin<ResultadoArchivoPedidoDto>.Fallo(
                    errorRegla.codigo,
                    errorRegla.mensaje,
                    errorRegla.campo));

            var fechaArchivo = RelojPedido.TruncarASegundosUtc(reloj.GetUtcNow());
            pedido.condicionAdministrativa = "Archivada";
            pedido.motivoArchivo = motivoLimpio;
            pedido.fechaArchivo = fechaArchivo;
            pedido.responsableArchivo = administradorId;

            return Task.FromResult(ResultadoOperacionAdmin<ResultadoArchivoPedidoDto>.Exito(new ResultadoArchivoPedidoDto(
                pedidoId: pedido.id,
                codigo: pedido.codigo,
                estado: pedido.estado,
                condicionAdministrativa: pedido.condicionAdministrativa,
                motivoArchivo: motivoLimpio,
                fechaArchivo: fechaArchivo,
                responsableArchivo: administradorId)));
        }, cancelacion);
    }

    internal static decimal CalcularTotalVersion(Pedido pedido, VersionCotizacion version)
    {
        var cantidadesPorDetalle = pedido.DetallePedido.ToDictionary(d => d.id, d => d.cantidad);
        decimal suma = 0m;
        foreach (var linea in version.DetalleCotizacion)
        {
            if (cantidadesPorDetalle.TryGetValue(linea.detalle, out var cantidad))
                suma += cantidad * linea.precioUnitarioAcordado;
        }
        return suma;
    }

    private static (decimal? totalVigente, decimal? anticipoExigible, decimal? saldoPendiente) CalcularResumenVigente(Pedido pedido)
    {
        if (pedido.versionCotizacionVigente is null)
            return (null, null, null);

        var vigente = pedido.VersionCotizacion.FirstOrDefault(v => v.id == pedido.versionCotizacionVigente.Value && v.decision == "Aceptada");
        if (vigente is null)
            return (null, null, null);

        var total = CalcularTotalVersion(pedido, vigente);
        var verificados = pedido.Pago.Where(g => g.estadoVerificacion == "Verificado").Sum(g => g.monto);
        var anticipo = ReglasAdministrativasPedido.CalcularAnticipoExigible(total);
        var saldo = ReglasAdministrativasPedido.CalcularSaldoPendiente(total, verificados);
        return (total, anticipo, saldo);
    }
}

internal static class RelojPedido
{
    public static DateTimeOffset TruncarASegundosUtc(DateTimeOffset instante)
    {
        var utc = instante.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero);
    }

    public static DateTimeOffset NoAnteriorA(DateTimeOffset actual, DateTimeOffset minimo)
    {
        var truncado = TruncarASegundosUtc(actual);
        var minimoUtc = TruncarASegundosUtc(minimo);
        return truncado < minimoUtc ? minimoUtc : truncado;
    }
}
