using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ImpactoTextil.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class GuardarProductoDto : IValidatableObject
{
    public string? nombre { get; init; }
    public string? color { get; init; }
    public string? talla { get; init; }
    public string? tela { get; init; }
    public string? imagenURL { get; init; }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? precioBase { get; init; }
    public bool? activo { get; init; }
    public string? version { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (campo, valor, maximo) in new[] { (nameof(nombre), nombre, 100),
            (nameof(color), color, 50), (nameof(talla), talla, 30), (nameof(tela), tela, 100) })
            if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Length > maximo || valor.Any(char.IsControl))
                yield return new($"Completa este campo sin superar {maximo} caracteres.", [campo]);
        if (string.IsNullOrWhiteSpace(imagenURL) || imagenURL.Length > 2048)
            yield return new("Selecciona una imagen pública de la prenda.", [nameof(imagenURL)]);
        if (precioBase is not { } precio || precio <= 0 || precio > 9999999999999999.99m || decimal.Round(precio, 2) != precio)
            yield return new("Indica un precio positivo con hasta dos decimales.", [nameof(precioBase)]);
        if (activo is null) yield return new("Indica la disponibilidad de la variante.", [nameof(activo)]);
        if (version is not null && (version.Length != 64 || !version.All(char.IsAsciiHexDigit)))
            yield return new("Recarga la variante antes de guardarla.", [nameof(version)]);
    }
}

public sealed record ProductoDto(int id, string nombre, string color, string talla, string tela,
    string imagenURL, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    decimal precioBase, bool activo, string version);
