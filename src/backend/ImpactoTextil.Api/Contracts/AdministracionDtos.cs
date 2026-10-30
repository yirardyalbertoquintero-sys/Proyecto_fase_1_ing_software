using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ImpactoTextil.Api.Services;

namespace ImpactoTextil.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class FiltroPedidosAdminDto : IValidatableObject
{
    public string? estado { get; init; }
    public string? condicionAdministrativa { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(estado) && !ReglasAdministrativasPedido.EstadosProduccionValidos.Contains(estado.Trim()))
            yield return new("El filtro de estado debe ser Pendiente, En producción, Listo para entrega o Entregado.", [nameof(estado)]);

        if (!string.IsNullOrWhiteSpace(condicionAdministrativa))
        {
            var condicion = condicionAdministrativa.Trim();
            if (condicion is not ("Activa" or "Archivada" or "Todas"))
                yield return new("La condición administrativa debe ser Activa, Archivada o Todas.", [nameof(condicionAdministrativa)]);
        }
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LineaCotizacionPropuestaDto : IValidatableObject
{
    public int detalle { get; init; }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal precioUnitarioAcordado { get; init; }
    public int? versionDiseno { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (detalle <= 0)
            yield return new("Identifica un detalle de pedido válido.", [nameof(detalle)]);

        if (!ReglasAdministrativasPedido.EsImporteUnitarioValido(precioUnitarioAcordado))
            yield return new("El precio unitario debe ser mayor que cero y tener como máximo dos decimales.", [nameof(precioUnitarioAcordado)]);

        if (versionDiseno is <= 0)
            yield return new("La versión de diseño asociada debe ser un identificador positivo.", [nameof(versionDiseno)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ProponerCotizacionDto : IValidatableObject
{
    public IReadOnlyList<LineaCotizacionPropuestaDto>? lineas { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (lineas is null || lineas.Count == 0)
        {
            yield return new("Incluye los precios unitarios propuestos para todos los detalles del pedido.", [nameof(lineas)]);
            yield break;
        }

        var ids = new HashSet<int>();
        for (var i = 0; i < lineas.Count; i++)
        {
            var linea = lineas[i];
            if (linea is null)
            {
                yield return new("Cada línea de la cotización es obligatoria.", [$"{nameof(lineas)}[{i}]"]);
                continue;
            }

            if (linea.detalle > 0 && !ids.Add(linea.detalle))
                yield return new("No repitas el mismo detalle dentro de una versión de cotización.", [$"{nameof(lineas)}[{i}].{nameof(LineaCotizacionPropuestaDto.detalle)}"]);

            var erroresLinea = new List<ValidationResult>();
            Validator.TryValidateObject(linea, new ValidationContext(linea), erroresLinea, true);
            foreach (var error in erroresLinea)
                yield return error;
        }
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DecidirCotizacionDto : IValidatableObject
{
    public string? decision { get; init; }
    public string? referenciaDecision { get; init; }
    public DateOnly? fechaEntregaAcordada { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var decisionLimpia = decision?.Trim();
        if (decisionLimpia is not ("Aceptada" or "Rechazada"))
            yield return new("La decisión sobre la cotización debe ser Aceptada o Rechazada.", [nameof(decision)]);

        if (!ValidacionTextoAdmin.EsTextoObligatorioValido(referenciaDecision, 250))
            yield return new("Registra la constancia o referencia de la decisión (1 a 250 caracteres).", [nameof(referenciaDecision)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegistrarVersionDisenoTextoDto : IValidatableObject
{
    public string? descripcion { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidacionTextoAdmin.EsTextoObligatorioValido(descripcion, 1000))
            yield return new("Escribe la descripción de la versión o la constancia «Sin personalización» (1 a 1000 caracteres).", [nameof(descripcion)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DecidirVersionDisenoDto : IValidatableObject
{
    public string? decision { get; init; }
    public string? referenciaDecision { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var decisionLimpia = decision?.Trim();
        if (decisionLimpia is not ("Aprobada" or "Rechazada"))
            yield return new("La decisión sobre la versión de diseño debe ser Aprobada o Rechazada.", [nameof(decision)]);

        if (!ValidacionTextoAdmin.EsTextoObligatorioValido(referenciaDecision, 250))
            yield return new("Registra la referencia del acuerdo de aprobación o rechazo (1 a 250 caracteres).", [nameof(referenciaDecision)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ArchivarPedidoDto : IValidatableObject
{
    public string? motivoArchivo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidacionTextoAdmin.EsTextoObligatorioValido(motivoArchivo, 250))
            yield return new("Indica el motivo documentado del archivo administrativo (1 a 250 caracteres).", [nameof(motivoArchivo)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ResolverPagoPendienteDto : IValidatableObject
{
    public string? decision { get; init; }
    public string? referencia { get; init; }
    public DateTimeOffset? fechaCobroConfirmada { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var decisionLimpia = decision?.Trim();
        if (decisionLimpia is not ("Verificado" or "Rechazado"))
            yield return new("La decisión de revisión del pago debe ser Verificado o Rechazado.", [nameof(decision)]);

        if (decisionLimpia == "Verificado")
        {
            if (!ValidacionTextoAdmin.EsTextoObligatorioValido(referencia, 100))
                yield return new("La referencia del cobro es obligatoria al verificar (1 a 100 caracteres).", [nameof(referencia)]);
        }
        else if (referencia is not null && !ValidacionTextoAdmin.EsTextoOpcionalValido(referencia, 100))
        {
            yield return new("La referencia no puede superar 100 caracteres ni contener caracteres de control.", [nameof(referencia)]);
        }
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegistrarCobroDirectoDto : IValidatableObject
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal monto { get; init; }
    public DateTimeOffset fecha { get; init; }
    public string? medio { get; init; }
    public string? referencia { get; init; }
    public Guid confirmacion { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ReglasAdministrativasPedido.EsImporteUnitarioValido(monto))
            yield return new("El monto del cobro debe ser mayor que cero y tener como máximo dos decimales.", [nameof(monto)]);

        if (fecha == default)
            yield return new("Indica la fecha y hora reales en que se recibió el cobro externo.", [nameof(fecha)]);

        var medioLimpio = medio?.Trim();
        if (medioLimpio is null || !ReglasAdministrativasPedido.MediosPagoValidos.Contains(medioLimpio))
            yield return new("El medio de pago debe ser Efectivo, Transferencia, Depósito o Tarjeta.", [nameof(medio)]);

        if (!ValidacionTextoAdmin.EsTextoObligatorioValido(referencia, 100))
            yield return new("Registra la referencia del cobro o la constancia interna de recepción (1 a 100 caracteres).", [nameof(referencia)]);

        if (confirmacion == Guid.Empty)
            yield return new("Incluye un identificador de confirmación válido para evitar cobros duplicados.", [nameof(confirmacion)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ActualizarEstadoProduccionDto : IValidatableObject
{
    public string? nuevoEstado { get; init; }
    public string? referenciaConfirmacionEntrega { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var estadoLimpio = nuevoEstado?.Trim();
        if (estadoLimpio is not ("En producción" or "Listo para entrega" or "Entregado"))
            yield return new("El estado solicitado debe ser En producción, Listo para entrega o Entregado.", [nameof(nuevoEstado)]);

        if (estadoLimpio == "Entregado")
        {
            if (!ValidacionTextoAdmin.EsTextoObligatorioValido(referenciaConfirmacionEntrega, 250))
                yield return new("Para marcar Entregado debes registrar la confirmación de recepción del cliente (1 a 250 caracteres).", [nameof(referenciaConfirmacionEntrega)]);
        }
        else if (referenciaConfirmacionEntrega is not null && !ValidacionTextoAdmin.EsTextoOpcionalValido(referenciaConfirmacionEntrega, 250))
        {
            yield return new("La referencia de confirmación no puede superar 250 caracteres ni incluir caracteres de control.", [nameof(referenciaConfirmacionEntrega)]);
        }
    }
}

internal static class ValidacionTextoAdmin
{
    public static bool EsTextoObligatorioValido(string? valor, int longitudMaxima)
    {
        var limpio = valor?.Trim();
        return !string.IsNullOrWhiteSpace(limpio)
            && limpio.Length <= longitudMaxima
            && !limpio.Any(EsCaracterControlNoPermitido);
    }

    public static bool EsTextoOpcionalValido(string valor, int longitudMaxima)
    {
        var limpio = valor.Trim();
        return limpio.Length <= longitudMaxima && !limpio.Any(EsCaracterControlNoPermitido);
    }

    private static bool EsCaracterControlNoPermitido(char caracter) =>
        char.IsControl(caracter) && caracter is not ('\r' or '\n' or '\t');
}

public sealed record ResumenPedidoAdminDto(
    int id,
    string codigo,
    int clienteId,
    string clienteNombre,
    string clienteCorreo,
    DateTimeOffset fechaOrden,
    DateOnly? fechaEntregaAcordada,
    DateTimeOffset? fechaEntregaReal,
    string estado,
    string modalidadEntrega,
    string condicionAdministrativa,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal totalReferencial,
    bool cotizacionAceptada,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? total,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal porcentajeAnticipo,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? anticipoExigible,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? saldoPendiente);

public sealed record DetallePedidoAdminDto(
    int id,
    int productoId,
    int cantidad,
    string talla,
    string color,
    string tela,
    string empaque,
    string? observaciones,
    string? ideaDiseno,
    bool tieneArchivoOriginal,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal precioBaseReferencia,
    int? versionDisenoVigente,
    bool disenoAprobado,
    DateTimeOffset? fechaAprobacionDiseno,
    int? responsableAprobacionDiseno,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? precioUnitarioAcordado,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? subtotal);

public sealed record LineaCotizacionAdminDto(
    int detalle,
    int cantidad,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal precioUnitarioAcordado,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal subtotal,
    int? versionDiseno);

public sealed record VersionCotizacionAdminDto(
    int id,
    int numeroVersion,
    DateTimeOffset fechaRegistro,
    int responsableRegistro,
    string decision,
    DateTimeOffset? fechaDecision,
    int? responsableDecision,
    string? referenciaDecision,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal total,
    IReadOnlyList<LineaCotizacionAdminDto> detalleImportes);

public sealed record VersionDisenoAdminDto(
    int id,
    int detalle,
    int numeroVersion,
    bool tieneArchivo,
    string? descripcion,
    DateTimeOffset fechaRegistro,
    int responsableRegistro,
    string decision,
    DateTimeOffset? fechaDecision,
    int? responsableDecision,
    string? referenciaDecision);

public sealed record PagoAdminDto(
    int id,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal monto,
    DateTimeOffset fecha,
    string medio,
    string? referencia,
    string estadoVerificacion,
    bool tieneComprobante,
    DateTimeOffset? fechaVerificacion,
    int? responsableVerificacion);

public sealed record ConstanciaAdminDto(
    string tipo,
    int registroOrigen,
    bool decision,
    DateTimeOffset fecha,
    int responsable,
    string referencia,
    int? versionCotizacion);

public sealed record ComprobacionSublimacionAdminDto(
    int id,
    int versionCotizacion,
    DateTimeOffset fecha,
    int responsable,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal montoVerificado,
    IReadOnlyList<int> versionesDiseno);

public sealed record ExpedientePedidoAdminDto(
    ResumenPedidoAdminDto pedido,
    string? direccionEntrega,
    string? motivoArchivo,
    DateTimeOffset? fechaArchivo,
    int? responsableArchivo,
    int? versionCotizacionVigente,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal pagosVerificadosAcumulados,
    IReadOnlyList<DetallePedidoAdminDto> detalles,
    IReadOnlyList<VersionCotizacionAdminDto> versionesCotizacion,
    IReadOnlyList<VersionDisenoAdminDto> versionesDiseno,
    IReadOnlyList<PagoAdminDto> pagos,
    IReadOnlyList<ConstanciaAdminDto> constancias,
    IReadOnlyList<ComprobacionSublimacionAdminDto> comprobacionesSublimacion);

public sealed record ResultadoCotizacionAdminDto(
    int pedidoId,
    int versionCotizacionId,
    int numeroVersion,
    string decision,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal totalPropuesto,
    int? versionCotizacionVigente,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? totalVigente,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? anticipoExigible,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? saldoPendiente,
    DateOnly? fechaEntregaAcordada);

public sealed record ResultadoVersionDisenoAdminDto(
    int pedidoId,
    int detalleId,
    int versionDisenoId,
    int numeroVersion,
    string decision,
    int? versionDisenoVigente,
    bool disenoAprobado);

public sealed record ResultadoArchivoPedidoDto(
    int pedidoId,
    string codigo,
    string estado,
    string condicionAdministrativa,
    string motivoArchivo,
    DateTimeOffset fechaArchivo,
    int responsableArchivo);

public sealed record ResultadoPagoAdminDto(
    int pedidoId,
    int pagoId,
    string estadoVerificacion,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal monto,
    string medio,
    string? referencia,
    DateTimeOffset fechaVerificacion,
    int responsableVerificacion,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal totalAceptado,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal pagosVerificadosAcumulados,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal anticipoExigible,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal saldoPendiente,
    bool esCreacionNueva);

public sealed record ResultadoAutorizacionSublimacionDto(
    int pedidoId,
    int comprobacionId,
    int versionCotizacion,
    IReadOnlyList<int> versionesDisenoComprobadas,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal totalAceptado,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal anticipoExigible,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal montoVerificado,
    DateTimeOffset fecha,
    int responsable);

public sealed record ResultadoEstadoProduccionDto(
    int pedidoId,
    string codigo,
    string estadoAnterior,
    string estadoActual,
    DateTimeOffset? fechaEntregaReal,
    int? constanciaEntregaId,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal totalAceptado,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal anticipoExigible,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal saldoPendiente);

public sealed record ArchivoDescargaDto(
    Stream contenido,
    string tipoContenido,
    string nombreDescarga,
    long tamanoBytes);
