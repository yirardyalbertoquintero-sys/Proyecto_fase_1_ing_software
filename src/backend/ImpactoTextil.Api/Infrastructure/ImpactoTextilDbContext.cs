using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Infrastructure;

// Base técnica para comprobar la conexión. Las entidades y migraciones se
// incorporarán después de revisar el modelo relacional con el equipo.
public sealed class ImpactoTextilDbContext(DbContextOptions<ImpactoTextilDbContext> opciones)
    : DbContext(opciones);
