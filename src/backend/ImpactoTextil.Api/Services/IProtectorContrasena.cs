namespace ImpactoTextil.Api.Services;

public interface IProtectorContrasena
{
    string CrearHash(string contrasena);
    bool Verificar(string contrasena, string hash);
}

public sealed class ProtectorContrasena : IProtectorContrasena
{
    public string CrearHash(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 12);
    public bool Verificar(string contrasena, string hash) => BCrypt.Net.BCrypt.Verify(contrasena, hash);
}
