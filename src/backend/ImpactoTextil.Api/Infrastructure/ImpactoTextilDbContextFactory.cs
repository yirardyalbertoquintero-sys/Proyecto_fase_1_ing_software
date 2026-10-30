using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImpactoTextil.Api.Infrastructure;

public sealed class ImpactoTextilDbContextFactory : IDesignTimeDbContextFactory<ImpactoTextilDbContext>
{
    public ImpactoTextilDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__ImpactoTextil")
            ?? throw new InvalidOperationException("Define ConnectionStrings__ImpactoTextil antes de utilizar migraciones.");
        var baseDatos = new SqlConnectionStringBuilder(conexion).InitialCatalog;
        if (string.IsNullOrWhiteSpace(baseDatos) || new[] { "master", "model", "msdb", "tempdb" }.Contains(baseDatos, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Las migraciones requieren una base propia de Impacto Textil.");
        return new ImpactoTextilDbContext(new DbContextOptionsBuilder<ImpactoTextilDbContext>()
            .UseSqlServer(conexion).Options);
    }
}
