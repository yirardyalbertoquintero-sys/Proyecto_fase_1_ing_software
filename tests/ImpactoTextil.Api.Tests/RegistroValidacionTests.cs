using System.ComponentModel.DataAnnotations;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Services;

namespace ImpactoTextil.Api.Tests;

public sealed class RegistroValidacionTests
{
    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(72, true)]
    [InlineData(73, false)]
    public void ContraseñasAscii_RespetanLosLimitesConfirmados(int longitud, bool valida)
    {
        var errores = Validar("Aa1!" + new string('x', longitud - 4));
        Assert.Equal(valida, errores.Count == 0);
    }

    [Fact]
    public void Unicode_CuentaCaracteresYBytesSinRecortar()
    {
        Assert.Empty(Validar("Aa1!" + new string('á', 34))); // 38 caracteres, 72 bytes.
        Assert.NotEmpty(Validar("Aa1!" + new string('á', 35)));
        Assert.NotEmpty(Validar("Aa1!🙂🙂🙂")); // Siete caracteres Unicode.
        Assert.Empty(Validar("Aa1!🙂🙂🙂🙂")); // Ocho caracteres Unicode.
        Assert.Empty(Validar("Una frase con espacios1!"));
        Assert.NotEmpty(Validar("        "));
        Assert.NotEmpty(Validar("Aa1!abc\n"));
    }

    [Theory]
    [InlineData("Textil8!", true)]
    [InlineData("textil8!", false)]
    [InlineData("TEXTIL8!", false)]
    [InlineData("Textil!!", false)]
    [InlineData("Textil88", false)]
    [InlineData("Tex8!", false)]
    [InlineData("Textil 8", false)] // El espacio no es un símbolo.
    [InlineData("Ábcdef1!", true)]
    public void Composicion_ExigeMayusculaMinusculaNumeroYSimbolo(string clave, bool valida) =>
        Assert.Equal(valida, Validar(clave).Count == 0);

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
