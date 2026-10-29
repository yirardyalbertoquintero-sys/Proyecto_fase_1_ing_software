namespace ImpactoTextil.Api.Services;

public interface IProtectorContrasena
{
    string CrearHash(string contrasena);
}

public sealed class ProtectorContrasena : IProtectorContrasena
{
    public string CrearHash(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 12);
}
