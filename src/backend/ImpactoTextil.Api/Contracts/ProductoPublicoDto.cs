using System.Text.Json.Serialization;

namespace ImpactoTextil.Api.Contracts;

public sealed record ProductoPublicoDto(int id, string nombre, string color, string talla, string tela,
    string? imagenURL, [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    decimal precioBase);
