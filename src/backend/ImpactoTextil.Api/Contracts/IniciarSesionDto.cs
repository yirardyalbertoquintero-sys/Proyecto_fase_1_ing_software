using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace ImpactoTextil.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class IniciarSesionDto : IValidatableObject
{
    public string? correo { get; init; }
    public string? contrasena { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var limpio = correo?.Trim();
        if (string.IsNullOrWhiteSpace(limpio) || limpio.Length > 150
            || limpio.Any(char.IsWhiteSpace) || limpio.Any(char.IsControl)
            || !new EmailAddressAttribute().IsValid(limpio))
            yield return new("Escribe un correo válido de hasta 150 caracteres.", [nameof(correo)]);
        // El acceso verifica la clave existente; no vuelve a aplicar la política de creación.
        if (string.IsNullOrWhiteSpace(contrasena) || Encoding.UTF8.GetByteCount(contrasena) > 72
            || contrasena.Any(char.IsControl))
            yield return new("Escribe tu contraseña completa, sin superar 72 bytes.", [nameof(contrasena)]);
    }
}

public sealed record SesionDto(int id, string nombre, string rol);
