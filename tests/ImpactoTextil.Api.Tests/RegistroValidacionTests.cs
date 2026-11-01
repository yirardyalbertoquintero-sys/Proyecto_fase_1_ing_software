using System.ComponentModel.DataAnnotations;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Services;

namespace ImpactoTextil.Api.Tests;

public sealed class RegistroValidacionTests
{
    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(72, true)]
    [InlineData(73, false)]
    public void ContraseñasAscii_RespetanLosLimitesConfirmados(int longitud, bool valida)
    {
        var errores = Validar(new string('a', longitud));
        Assert.Equal(valida, errores.Count == 0);
    }

    [Fact]
    public void Unicode_CuentaCaracteresYBytesSinRecortar()
    {
        Assert.Empty(Validar(new string('á', 36))); // 36 caracteres, 72 bytes.
        Assert.NotEmpty(Validar(new string('á', 37)));
        Assert.NotEmpty(Validar(string.Concat(Enumerable.Repeat("🙂", 14)))); // No son 28 caracteres.
        Assert.Empty(Validar("una frase con espacios"));
        Assert.NotEmpty(Validar("               "));
    }

    [Fact]
    public void BCrypt_GeneraSalesDistintasYVerificaLaContraseñaCompleta()
    {
        var protector = new ProtectorContrasena();
        const string clave = "frase de prueba con espacios";
        var primera = protector.CrearHash(clave);
        var segunda = protector.CrearHash(clave);
        Assert.NotEqual(primera, segunda);
        Assert.Equal(60, primera.Length);
        Assert.True(BCrypt.Net.BCrypt.Verify(clave, primera));
        Assert.True(BCrypt.Net.BCrypt.Verify(clave, segunda));
        Assert.False(BCrypt.Net.BCrypt.Verify(clave + "x", primera));
    }

    private static List<ValidationResult> Validar(string clave)
    {
        var solicitud = new RegistrarClienteDto
        { nombre = "Cliente ficticio", correo = "persona@example.test", contrasena = clave, aceptacionTerminos = true };
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(solicitud, new ValidationContext(solicitud), resultados, true);
        return resultados;
    }
}
