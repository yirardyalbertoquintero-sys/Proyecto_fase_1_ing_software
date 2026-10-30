using System.Security.Cryptography;
using System.Text.Json;
using System.Globalization;
using ImpactoTextil.Api.Domain.Entities;

namespace ImpactoTextil.Api.Domain;

public static class ProductoVersion
{
    public static string Calcular(Producto p) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
        new { p.id, p.nombre, p.color, p.talla, p.tela, p.imagenURL,
            precioBase = p.precioBase.ToString("F2", CultureInfo.InvariantCulture), p.activo })));
}
