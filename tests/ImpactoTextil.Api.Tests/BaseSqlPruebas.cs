using ImpactoTextil.Api.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Tests;

public sealed class PruebaSqlAttribute : FactAttribute
{
    public PruebaSqlAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IMPACTOTEXTIL_PRUEBAS_SQL")))
            Skip = "Ejecutar scripts/Test-Registro.ps1 para incluir SQL Server real.";
    }
}

public sealed class BaseSqlPruebas : IAsyncLifetime
{
    private readonly string nombreBase = "ImpactoTextil_RegistroPrueba_" + Guid.NewGuid().ToString("N");
    private string? conexionMaestra;
    private bool creada;
    public string Conexion { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var configurada = Environment.GetEnvironmentVariable("IMPACTOTEXTIL_PRUEBAS_SQL");
        if (string.IsNullOrWhiteSpace(configurada)) return;
        var opciones = new SqlConnectionStringBuilder(configurada) { InitialCatalog = "master" };
        conexionMaestra = opciones.ConnectionString;
        await EjecutarMaestra($"CREATE DATABASE [{nombreBase}];");
        creada = true;
        opciones.InitialCatalog = nombreBase;
        Conexion = opciones.ConnectionString;
        try
        {
            await using var contexto = CrearContexto();
            await contexto.Database.MigrateAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public ImpactoTextilDbContext CrearContexto() => new(new DbContextOptionsBuilder<ImpactoTextilDbContext>()
        .UseSqlServer(Conexion).Options);

    public async Task DisposeAsync()
    {
        // Solo se elimina la base cuyo nombre generó esta misma instancia.
        if (!creada) return;
        SqlConnection.ClearAllPools();
        await EjecutarMaestra($"ALTER DATABASE [{nombreBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nombreBase}];");
        creada = false;
    }

    private async Task EjecutarMaestra(string sql)
    {
        await using var conexion = new SqlConnection(conexionMaestra);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion) { CommandTimeout = 60 };
        await comando.ExecuteNonQueryAsync();
    }
}
