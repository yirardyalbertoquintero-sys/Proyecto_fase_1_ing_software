using System.ComponentModel.DataAnnotations;
using System.Text;
using ImpactoTextil.Api.Services;

namespace ImpactoTextil.Api.Infrastructure;

public static class InicializarAdministrador
{
    public static async Task<int> EjecutarAsync(IServiceProvider servicios)
    {
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Ejecuta este comando en una terminal interactiva. No pases contraseñas por argumentos ni archivos.");
            return 1;
        }
        Console.WriteLine("Creación controlada del primer Administrador. No modifica cuentas existentes.");
        Console.Write("Nombre: "); var nombre = Console.ReadLine() ?? "";
        Console.Write("Correo: "); var correo = Console.ReadLine() ?? "";
        Console.Write("Contraseña (mínimo 15 caracteres, no se mostrará): "); var clave = LeerClave();
        Console.Write("Repite la contraseña: "); var confirmacion = LeerClave();
        if (clave != confirmacion) { Console.Error.WriteLine("Las contraseñas no coinciden. No se creó la cuenta."); return 1; }
        try
        {
            using var alcance = servicios.CreateScope();
            var id = await alcance.ServiceProvider.GetRequiredService<AdministradorInicialService>()
                .CrearAsync(nombre, correo, clave, CancellationToken.None);
            Console.WriteLine($"Administrador creado. Identificador: {id}. Ya puedes iniciar sesión.");
            return 0;
        }
        catch (ValidationException error) { Console.Error.WriteLine(error.Message); return 1; }
        catch (InvalidOperationException error) when (error.Message.StartsWith("Ya existe") || error.Message.StartsWith("Ese correo"))
        { Console.Error.WriteLine(error.Message); return 1; }
        catch { Console.Error.WriteLine("No se pudo confirmar la creación. Revisa la conexión y el estado de la cuenta antes de reintentar."); return 1; }
    }

    private static string LeerClave()
    {
        var texto = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) break;
            if (tecla.Key == ConsoleKey.Backspace) { if (texto.Length > 0) texto.Length--; }
            else if (!char.IsControl(tecla.KeyChar) && texto.Length < 256) texto.Append(tecla.KeyChar);
        }
        Console.WriteLine();
        return texto.ToString();
    }
}
