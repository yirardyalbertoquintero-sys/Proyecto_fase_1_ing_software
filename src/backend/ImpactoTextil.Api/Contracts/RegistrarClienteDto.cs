using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace ImpactoTextil.Api.Contracts;

// Los campos ajenos al contrato se rechazan, incluido un rol enviado por el navegador.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegistrarClienteDto : IValidatableObject
{
    public string? nombre { get; init; }
    public string? correo { get; init; }
    public string? contrasena { get; init; }
    public bool aceptacionTerminos { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var nombreLimpio = nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombreLimpio) || nombreLimpio.Length > 100 || nombreLimpio.Any(char.IsControl))
            yield return new("Escribe un nombre de entre 1 y 100 caracteres.", [nameof(nombre)]);

        var correoLimpio = correo?.Trim();
        if (string.IsNullOrWhiteSpace(correoLimpio) || correoLimpio.Length > 150
            || correoLimpio.Any(char.IsWhiteSpace) || correoLimpio.Any(char.IsControl)
            || !new EmailAddressAttribute().IsValid(correoLimpio))
            yield return new("Escribe un correo válido de hasta 150 caracteres.", [nameof(correo)]);

        // Se cuentan caracteres Unicode y bytes UTF-8. No recortar ni modificar la contraseña.
        if (string.IsNullOrWhiteSpace(contrasena) || contrasena.EnumerateRunes().Count() < 8)
            yield return new("Usa al menos 8 caracteres.", [nameof(contrasena)]);
        else if (Encoding.UTF8.GetByteCount(contrasena) > 72)
            yield return new("La contraseña es demasiado larga: admite hasta 72 bytes; las tildes y los emojis pueden ocupar más de uno.", [nameof(contrasena)]);
        else if (contrasena.Any(char.IsControl))
            yield return new("La contraseña no puede contener saltos de línea ni caracteres de control.", [nameof(contrasena)]);
        else if (!contrasena.EnumerateRunes().Any(Rune.IsUpper)
            || !contrasena.EnumerateRunes().Any(Rune.IsLower)
            || !contrasena.EnumerateRunes().Any(Rune.IsDigit)
            || !contrasena.EnumerateRunes().Any(caracter => Rune.IsPunctuation(caracter) || Rune.IsSymbol(caracter)))
            yield return new("Incluye al menos una mayúscula, una minúscula, un número y un símbolo.", [nameof(contrasena)]);

        if (!aceptacionTerminos)
            yield return new("Lee y acepta los términos y el aviso de privacidad para continuar.", [nameof(aceptacionTerminos)]);
    }
}

public sealed record ClienteCreadoDto(int id, string rol);
