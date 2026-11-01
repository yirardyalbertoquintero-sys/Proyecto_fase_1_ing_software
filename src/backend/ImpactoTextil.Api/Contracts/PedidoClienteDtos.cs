using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ImpactoTextil.Api.Services;

namespace ImpactoTextil.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegistrarComprobanteDto : IValidatableObject
{
    public Guid confirmacion { get; init; }
    public int clienteConfirmado { get; init; }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal monto { get; init; }
    public string? medio { get; init; }
    public string? referencia { get; init; }
    public DateTimeOffset fecha { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (confirmacion == Guid.Empty || clienteConfirmado <= 0)
            yield return new("Comprueba tu sesión y confirma el envío.");
        if (!ReglasAdministrativasPedido.EsImporteUnitarioValido(monto))
            yield return new("El monto debe ser positivo, con hasta dos decimales.", [nameof(monto)]);
        if (medio is null || !ReglasAdministrativasPedido.MediosPagoValidos.Contains(medio))
            yield return new("Selecciona un medio de cobro permitido.", [nameof(medio)]);
        if (string.IsNullOrWhiteSpace(referencia) || referencia.Length > 100 || referencia.Any(char.IsControl))
            yield return new("Escribe una referencia de hasta 100 caracteres, sin controles.", [nameof(referencia)]);
        if (fecha == default || fecha.Year < 1900 || fecha.Year > 9998)
            yield return new("Escribe una fecha de cobro válida.", [nameof(fecha)]);
    }
}

public sealed record ResumenPedidoClienteDto(int id, string codigo, DateTimeOffset fechaOrden,
    string estado, string condicionAdministrativa, string modalidadEntrega, DateOnly? fechaEntregaAcordada,
    DateTimeOffset? fechaEntregaReal, bool cotizacionAceptada,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal totalReferencial,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? total,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? anticipoExigible,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal? saldoPendiente);

public sealed record PrendaClienteDto(int id, int cantidad, string talla, string color, string tela, string empaque,
    string? ideaDiseno, string? observaciones, bool disenoAprobado, int? versionDisenoVigente);
public sealed record CotizacionClienteDto(int id, int numeroVersion, DateTimeOffset fechaRegistro,
    string decision, DateTimeOffset? fechaDecision,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal total,
    IReadOnlyList<LineaCotizacionAdminDto> detalleImportes);
public sealed record DisenoClienteDto(int id, int detalle, int numeroVersion, string? descripcion,
    string decision, DateTimeOffset fechaRegistro, DateTimeOffset? fechaDecision);
public sealed record PagoClienteDto(int id,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal monto,
    string medio, string? referencia, DateTimeOffset fecha, string estadoVerificacion,
    DateTimeOffset? fechaVerificacion, bool tieneComprobante);
public sealed record ExpedientePedidoClienteDto(ResumenPedidoClienteDto pedido, string? direccionEntrega,
    string? motivoArchivo, DateTimeOffset? fechaArchivo, int? versionCotizacionVigente,
    IReadOnlyList<PrendaClienteDto> detalles, IReadOnlyList<CotizacionClienteDto> versionesCotizacion,
    IReadOnlyList<DisenoClienteDto> versionesDiseno, IReadOnlyList<PagoClienteDto> pagos);
public sealed record ComprobanteRecibidoDto(int pedidoId, int pagoId, string estadoVerificacion, bool esCreacionNueva);
