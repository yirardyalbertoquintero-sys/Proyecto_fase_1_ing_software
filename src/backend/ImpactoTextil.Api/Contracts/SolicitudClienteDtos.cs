using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ImpactoTextil.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegistrarSolicitudDto : IValidatableObject
{
    public Guid confirmacion { get; init; }
    public int clienteConfirmado { get; init; }
    public string? modalidadEntrega { get; init; }
    public string? direccionEntrega { get; init; }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? totalReferencialConfirmado { get; init; }
    public List<ConfiguracionPrendaDto?>? detalles { get; init; }

    // Presupuestos técnicos locales; no son cantidades mínimas del negocio.
    public const int MaximoDetalles = 20;
    public const long MaximoArchivosTotales = 20_000_000;
    public const long MaximoPeticion = 20_500_000;
    public const decimal MaximoTotal = 99999999999999999999999999.99m;
    public const string EmpaqueActual = "Bolsa estándar";

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (confirmacion == Guid.Empty) yield return new("Confirma la solicitud antes de enviarla.", [nameof(confirmacion)]);
        if (clienteConfirmado <= 0) yield return new("Comprueba tu sesión antes de confirmar.", [nameof(clienteConfirmado)]);
        if (modalidadEntrega is not ("Retiro" or "Domicilio")) yield return new("Selecciona Retiro o Domicilio.", [nameof(modalidadEntrega)]);
        if (!Texto(direccionEntrega, 250) || (modalidadEntrega == "Domicilio" && string.IsNullOrWhiteSpace(direccionEntrega)))
            yield return new("Para domicilio escribe una dirección de hasta 250 caracteres, sin controles.", [nameof(direccionEntrega)]);
        if (totalReferencialConfirmado is not { } total || total <= 0 || total > MaximoTotal || decimal.Round(total, 2) != total)
            yield return new("Revisa el importe de referencia antes de confirmar.", [nameof(totalReferencialConfirmado)]);
        if (detalles is null || detalles.Count is < 1 or > MaximoDetalles)
        { yield return new($"Agrega entre 1 y {MaximoDetalles} configuraciones.", [nameof(detalles)]); yield break; }
        for (var i = 0; i < detalles.Count; i++)
        {
            var d = detalles[i];
            if (d is null) { yield return new("Configuración incompleta.", [$"detalles[{i}]"]); continue; }
            if (d.producto <= 0 || d.cantidad <= 0) yield return new("Selecciona una variante y una cantidad positiva.", [$"detalles[{i}]"]);
            if (!Requerido(d.talla, 30) || !Requerido(d.color, 50) || !Requerido(d.tela, 100))
                yield return new("Revisa las características de la variante.", [$"detalles[{i}]"]);
            if (d.empaque != EmpaqueActual) yield return new("El empaque disponible es bolsa estándar.", [$"detalles[{i}].empaque"]);
            if (d.precioBaseConfirmado is not { } precio || precio <= 0 || precio > 9999999999999999.99m || decimal.Round(precio, 2) != precio)
                yield return new("Vuelve a consultar el precio de la variante.", [$"detalles[{i}].precioBaseConfirmado"]);
            if (!Texto(d.ideaDiseno, 1000) || !Texto(d.observaciones, 1000))
                yield return new("Idea y observaciones admiten hasta 1000 caracteres sin controles.", [$"detalles[{i}]"]);
            if (d.archivo is not null && (d.archivo.Length is < 1 or > 60 || !d.archivo.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')))
                yield return new("El identificador del archivo no es válido.", [$"detalles[{i}].archivo"]);
            if (d.sinPersonalizacion && (d.archivo is not null || !string.IsNullOrWhiteSpace(d.ideaDiseno)))
                yield return new("Sin personalización no lleva imagen ni idea de estampado.", [$"detalles[{i}]"]);
            if (!d.sinPersonalizacion && d.archivo is null && string.IsNullOrWhiteSpace(d.ideaDiseno))
                yield return new("Adjunta un diseño, describe tu idea o indica Sin personalización.", [$"detalles[{i}]"]);
        }
        var archivos = detalles.Where(d => d?.archivo is not null).Select(d => d!.archivo!).ToArray();
        if (archivos.Distinct(StringComparer.Ordinal).Count() != archivos.Length)
            yield return new("Cada configuración debe identificar su propio archivo.", [nameof(detalles)]);
    }

    private static bool Texto(string? value, int max) => value is null || (value.Length <= max && !value.Any(char.IsControl));
    private static bool Requerido(string? value, int max) => !string.IsNullOrWhiteSpace(value) && Texto(value, max);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ConfiguracionPrendaDto
{
    public int producto { get; init; }
    public int cantidad { get; init; }
    public string? talla { get; init; }
    public string? color { get; init; }
    public string? tela { get; init; }
    public string? empaque { get; init; }
    public bool sinPersonalizacion { get; init; }
    public string? ideaDiseno { get; init; }
    public string? observaciones { get; init; }
    public string? archivo { get; init; }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? precioBaseConfirmado { get; init; }
}

public sealed record SolicitudCreadaDto(int id, string codigo, string estado,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)] decimal totalReferencial,
    DateTimeOffset fechaOrden, bool esCreacionNueva);
