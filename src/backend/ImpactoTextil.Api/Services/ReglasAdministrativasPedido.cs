namespace ImpactoTextil.Api.Services;

public sealed record EstadoDetalleEvaluacion(
    int detalleId,
    int? versionDisenoVigente,
    bool versionVigenteAprobadaYPropia);

public static class ReglasAdministrativasPedido
{
    public const decimal PorcentajeAnticipoFijo = 50.00m;
    public const decimal MaximoImporteDecimal18_2 = 9_999_999_999_999_999.99m;

    public static readonly IReadOnlySet<string> EstadosProduccionValidos = new HashSet<string>(StringComparer.Ordinal)
    {
        "Pendiente",
        "En producción",
        "Listo para entrega",
        "Entregado"
    };

    public static readonly IReadOnlySet<string> MediosPagoValidos = new HashSet<string>(StringComparer.Ordinal)
    {
        "Efectivo",
        "Transferencia",
        "Depósito",
        "Tarjeta"
    };

    public static bool TieneMaximoDosDecimales(decimal importe) =>
        decimal.Round(importe, 2, MidpointRounding.ToZero) == importe;

    public static bool EsImporteUnitarioValido(decimal importe) =>
        importe > 0m && importe <= MaximoImporteDecimal18_2 && TieneMaximoDosDecimales(importe);

    public static decimal CalcularSubtotal(int cantidad, decimal precioUnitarioAcordado)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser un entero positivo.");
        if (!EsImporteUnitarioValido(precioUnitarioAcordado))
            throw new ArgumentOutOfRangeException(nameof(precioUnitarioAcordado), "El precio unitario debe ser positivo y con máximo dos decimales.");

        return cantidad * precioUnitarioAcordado;
    }

    /// <summary>
    /// RF-11 y Diccionario 4.3: anticipoExigible = total aceptado × 0.50;
    /// si resulta una fracción de centavo, redondea hacia arriba al centavo siguiente.
    /// Sin excepciones operativas del 0 % o 30 %.
    /// </summary>
    public static decimal CalcularAnticipoExigible(decimal totalAceptado)
    {
        if (totalAceptado <= 0m || !TieneMaximoDosDecimales(totalAceptado))
            throw new ArgumentOutOfRangeException(nameof(totalAceptado), "El total aceptado debe ser positivo y tener como máximo dos decimales.");

        return Math.Ceiling(totalAceptado * PorcentajeAnticipoFijo) / 100m;
    }

    public static decimal CalcularSaldoPendiente(decimal totalAceptado, decimal pagosVerificadosAcumulados)
    {
        if (totalAceptado <= 0m || !TieneMaximoDosDecimales(totalAceptado))
            throw new ArgumentOutOfRangeException(nameof(totalAceptado), "El total aceptado debe ser positivo y tener como máximo dos decimales.");
        if (pagosVerificadosAcumulados < 0m || !TieneMaximoDosDecimales(pagosVerificadosAcumulados))
            throw new ArgumentOutOfRangeException(nameof(pagosVerificadosAcumulados), "Los pagos verificados acumulados no pueden ser negativos ni tener más de dos decimales.");
        if (pagosVerificadosAcumulados > totalAceptado)
            throw new InvalidOperationException("Los pagos verificados acumulados no pueden superar el total aceptado.");

        return totalAceptado - pagosVerificadosAcumulados;
    }

    public static ErrorOperacionAdmin? ValidarSiguienteEstadoSecuencia(
        string estadoActual,
        string nuevoEstado,
        string condicionAdministrativa)
    {
        if (condicionAdministrativa == "Archivada")
            return new(CodigoErrorAdmin.ConflictoEstado, "Una solicitud archivada no admite avances de producción ni entrega.", "condicionAdministrativa");

        var siguienteEsperado = estadoActual switch
        {
            "Pendiente" => "En producción",
            "En producción" => "Listo para entrega",
            "Listo para entrega" => "Entregado",
            _ => null
        };

        if (siguienteEsperado is null || !string.Equals(siguienteEsperado, nuevoEstado, StringComparison.Ordinal))
        {
            return new(
                CodigoErrorAdmin.ConflictoEstado,
                $"Transición inválida desde «{estadoActual}» hacia «{nuevoEstado}». La secuencia permitida es Pendiente → En producción → Listo para entrega → Entregado.",
                "nuevoEstado");
        }

        return null;
    }

    public static ErrorOperacionAdmin? EvaluarInicioProduccion(
        string condicionAdministrativa,
        string estadoActual,
        bool tieneCotizacionAceptada,
        decimal? totalAceptado,
        DateOnly? fechaEntregaAcordada,
        IReadOnlyList<EstadoDetalleEvaluacion> detalles,
        decimal pagosVerificadosAcumulados)
    {
        var errorTransicion = ValidarSiguienteEstadoSecuencia(estadoActual, "En producción", condicionAdministrativa);
        if (errorTransicion is not null)
            return errorTransicion;

        if (!tieneCotizacionAceptada || totalAceptado is null or <= 0m)
            return new(CodigoErrorAdmin.ConflictoEstado, "No se puede iniciar producción sin una cotización aceptada vigente.", "versionCotizacionVigente");

        if (fechaEntregaAcordada is null)
            return new(CodigoErrorAdmin.ConflictoEstado, "Registra primero la fecha de entrega acordada con el cliente antes de iniciar producción.", "fechaEntregaAcordada");

        if (detalles.Count == 0)
            return new(CodigoErrorAdmin.ConflictoEstado, "El pedido no contiene detalles válidos para producir.", "detalles");

        if (detalles.Any(d => d.versionDisenoVigente is null || !d.versionVigenteAprobadaYPropia))
            return new(CodigoErrorAdmin.ConflictoEstado, "Todos los detalles deben contar con su versión vigente de diseño aprobada (o constancia «Sin personalización» aprobada) antes de iniciar producción.", "versionDisenoVigente");

        var anticipoExigible = CalcularAnticipoExigible(totalAceptado.Value);
        if (pagosVerificadosAcumulados < anticipoExigible)
        {
            var faltante = anticipoExigible - pagosVerificadosAcumulados;
            return new(
                CodigoErrorAdmin.ConflictoEstado,
                $"Los pagos verificados (C${pagosVerificadosAcumulados:F2}) no cubren el anticipo mínimo del 50 % (C${anticipoExigible:F2}). Faltan C${faltante:F2} verificados para iniciar producción.",
                "anticipoExigible");
        }

        return null;
    }

    /// <summary>
    /// CU-12 / RF-10 / §4.6: La autorización de sublimación es un control dentro de «En producción» (no un quinto estado).
    /// Si aumenta el total aceptado o cambia un diseño, exige cubrir el nuevo 50 % y tener aprobadas todas las versiones vigentes.
    /// </summary>
    public static ErrorOperacionAdmin? EvaluarAutorizacionSublimacion(
        string condicionAdministrativa,
        string estadoActual,
        bool tieneCotizacionAceptada,
        decimal? totalAceptado,
        IReadOnlyList<EstadoDetalleEvaluacion> detalles,
        decimal pagosVerificadosAcumulados)
    {
        if (condicionAdministrativa == "Archivada")
            return new(CodigoErrorAdmin.ConflictoEstado, "Una solicitud archivada no admite autorización de sublimación.", "condicionAdministrativa");

        if (!string.Equals(estadoActual, "En producción", StringComparison.Ordinal))
            return new(CodigoErrorAdmin.ConflictoEstado, "La autorización de sublimación solo se evalúa cuando el pedido está En producción.", "estado");

        if (!tieneCotizacionAceptada || totalAceptado is null or <= 0m)
            return new(CodigoErrorAdmin.ConflictoEstado, "Se requiere una cotización aceptada vigente para autorizar la sublimación.", "versionCotizacionVigente");

        if (detalles.Count == 0 || detalles.Any(d => d.versionDisenoVigente is null || !d.versionVigenteAprobadaYPropia))
            return new(CodigoErrorAdmin.ConflictoEstado, "No se autoriza la sublimación mientras exista alguna prenda sin versión de diseño vigente aprobada.", "versionDisenoVigente");

        var anticipoExigible = CalcularAnticipoExigible(totalAceptado.Value);
        if (pagosVerificadosAcumulados < anticipoExigible)
        {
            var faltante = anticipoExigible - pagosVerificadosAcumulados;
            return new(
                CodigoErrorAdmin.ConflictoEstado,
                $"No se autoriza continuar la sublimación: con el total aceptado vigente de C${totalAceptado.Value:F2}, el anticipo mínimo del 50 % es C${anticipoExigible:F2} y solo hay C${pagosVerificadosAcumulados:F2} verificados. Faltan C${faltante:F2}.",
                "anticipoExigible");
        }

        return null;
    }

    public static ErrorOperacionAdmin? EvaluarPasoAListoParaEntrega(
        string condicionAdministrativa,
        string estadoActual,
        bool tieneCotizacionAceptada,
        decimal? totalAceptado,
        IReadOnlyList<EstadoDetalleEvaluacion> detalles,
        decimal pagosVerificadosAcumulados,
        bool tieneComprobacionSublimacionVigente)
    {
        var errorTransicion = ValidarSiguienteEstadoSecuencia(estadoActual, "Listo para entrega", condicionAdministrativa);
        if (errorTransicion is not null)
            return errorTransicion;

        var errorSublimacion = EvaluarAutorizacionSublimacion(
            condicionAdministrativa,
            estadoActual,
            tieneCotizacionAceptada,
            totalAceptado,
            detalles,
            pagosVerificadosAcumulados);
        if (errorSublimacion is not null)
            return errorSublimacion;

        if (!tieneComprobacionSublimacionVigente)
            return new(CodigoErrorAdmin.ConflictoEstado, "Registra primero la comprobación de sublimación para la cotización y los diseños vigentes antes de marcar el pedido como Listo para entrega.", "comprobacionSublimacion");

        return null;
    }

    /// <summary>
    /// CU-12 / RF-10 / §5.5: Política propuesta para el MVP académico (pendiente de validación comercial para operación real):
    /// exigir saldo pendiente C$0.00 y confirmación registrada del cliente.
    /// </summary>
    public static ErrorOperacionAdmin? EvaluarEntrega(
        string condicionAdministrativa,
        string estadoActual,
        bool tieneCotizacionAceptada,
        decimal? totalAceptado,
        decimal pagosVerificadosAcumulados,
        string? referenciaConfirmacionEntrega)
    {
        var errorTransicion = ValidarSiguienteEstadoSecuencia(estadoActual, "Entregado", condicionAdministrativa);
        if (errorTransicion is not null)
            return errorTransicion;

        if (!tieneCotizacionAceptada || totalAceptado is null or <= 0m)
            return new(CodigoErrorAdmin.ConflictoEstado, "El pedido debe contar con una cotización aceptada vigente antes de registrarse como Entregado.", "versionCotizacionVigente");

        if (string.IsNullOrWhiteSpace(referenciaConfirmacionEntrega))
            return new(CodigoErrorAdmin.Validacion, "Registra la confirmación de recepción del cliente para completar la entrega.", "referenciaConfirmacionEntrega");

        var saldoPendiente = CalcularSaldoPendiente(totalAceptado.Value, pagosVerificadosAcumulados);
        if (saldoPendiente > 0m)
        {
            return new(
                CodigoErrorAdmin.ConflictoEstado,
                $"Política vigente de la demostración académica (SRS §5.5): para marcar Entregado el saldo pendiente debe ser C$0.00 y actualmente resta C${saldoPendiente:F2}.",
                "saldoPendiente");
        }

        return null;
    }

    /// <summary>
    /// CU-10 / Regla 13 de §3.3 y CK_Pedido_archivo: solo solicitudes Activas, en estado Pendiente,
    /// sin cotización aceptada y sin cobros verificados ni pendientes de resolver.
    /// </summary>
    public static ErrorOperacionAdmin? EvaluarArchivoAdministrativo(
        string condicionAdministrativa,
        string estadoActual,
        bool tieneAlgunaCotizacionAceptada,
        bool tienePagosVerificadosOPendientes)
    {
        if (condicionAdministrativa == "Archivada")
            return new(CodigoErrorAdmin.ConflictoEstado, "La solicitud ya se encuentra archivada.", "condicionAdministrativa");

        if (!string.Equals(estadoActual, "Pendiente", StringComparison.Ordinal))
            return new(CodigoErrorAdmin.ConflictoEstado, "Solo se pueden archivar solicitudes que permanezcan en estado Pendiente sin producción iniciada.", "estado");

        if (tieneAlgunaCotizacionAceptada)
            return new(CodigoErrorAdmin.ConflictoEstado, "No se puede archivar un pedido que ya cuenta con una cotización aceptada.", "versionCotizacionVigente");

        if (tienePagosVerificadosOPendientes)
            return new(CodigoErrorAdmin.ConflictoEstado, "No se puede archivar un pedido con pagos verificados o comprobantes pendientes de revisión.", "pagos");

        return null;
    }
}
