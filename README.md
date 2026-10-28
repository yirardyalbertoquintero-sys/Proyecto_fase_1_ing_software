Proyecto ing software

# Impacto Textil — inicio de las fases 3 y 4

Aplicación de pedidos de camisetas personalizadas por sublimación. Este avance prepara la parte inicial de **Yirardy**: diseño de datos, normalización, arquitectura, entorno común y comprobación técnica de la API.

**Estado:** prototipo SQL y API mínima comprobados localmente. Registro, acceso, catálogo, pedidos, pagos y pantallas siguen pendientes de implementación. La aplicación todavía no está publicada.

## Empezar por aquí

1. Leer [la fuente vigente](docs/FUENTE_VIGENTE.md) y `AGENTS.md`. El DOCX aprobado se conserva intacto. Los archivos anteriores del repositorio no sustituyen sus requisitos.
2. Revisar [el diseño de fase 3](docs/fase-3/00-inicio.md), el [flujo del equipo](docs/fase-3/07-flujo-equipo.md) y el [avance comprobado](docs/seguimiento/avance-yirardy.md).
3. Abrir [las cinco páginas editables](docs/fase-3/diagramas/Impacto_Textil_Fase_3.drawio) en draw.io. Las versiones SVG y Mermaid están en la misma carpeta.
4. Ejecutar las comprobaciones siguientes desde la raíz del repositorio.

## Herramientas necesarias

- SDK .NET 10.0.402, fijado en `global.json` con actualización de parches permitida.
- SQL Server y `sqlcmd` para ensayar el modelo. Las pruebas HTTP se ejecutan sin SQL Server.
- PowerShell para `scripts/Test-Modelo.ps1`.
- Python 3 solo si se desea regenerar los SVG y el archivo draw.io; ese generador no requiere paquetes externos.

Frontend previsto: HTML, CSS y JavaScript puro. Backend: ASP.NET Core, EF Core, Repository y DTO. Destino previsto: MonsterASP.NET Free, sujeto a una prueba real de alojamiento.

## Restaurar, compilar y probar la API

```powershell
dotnet restore ImpactoTextil.slnx --locked-mode
dotnet build ImpactoTextil.slnx --no-restore
dotnet test ImpactoTextil.slnx --no-build
dotnet list ImpactoTextil.slnx package --vulnerable --include-transitive
```

Los archivos `packages.lock.json` fijan también las dependencias transitivas. Si se cambia intencionalmente una dependencia, regenerar el bloqueo, revisar el cambio y repetir las comprobaciones. `Directory.Build.props` trata las alertas de vulnerabilidad moderada, alta o crítica como errores de restauración.

## Ensayar el modelo de datos

```powershell
./scripts/Test-Modelo.ps1 -Servidor localhost
```

El usuario de Windows necesita permiso para crear una base de prueba. Para Express, usar el nombre real de su instancia, por ejemplo `-Servidor '.\SQLEXPRESS'`. El script crea una base temporal con nombre único, aplica el modelo, ejecuta 29 comprobaciones y elimina únicamente esa base al finalizar. Usa datos ficticios; no sirve para cargar datos de producción.

`database/001_modelo_fase3.sql` es un **prototipo de diseño**. Aún no existen entidades mapeadas ni migraciones EF Core. Después de revisar el diseño con el equipo, la migración inicial acordada será la autoridad del esquema de la aplicación. No ejecutar este prototipo encima de una base existente.

## Ejecutar la comprobación de disponibilidad

```powershell
dotnet run --project src/backend/ImpactoTextil.Api --launch-profile https
```

La ruta es `https://localhost:7105/api/disponibilidad`. Sin conexión configurada responde **503** con un mensaje comprensible; eso indica que la API arrancó, pero SQL aún no está disponible para ella. Si falta el certificado de desarrollo, generarlo con `dotnet dev-certs https`; la confianza del certificado local se gestiona en cada equipo.

Para una prueba técnica de conectividad con la instancia local, antes de arrancar se puede definir en la misma consola:

```powershell
$env:ConnectionStrings__ImpactoTextil = 'Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5'
dotnet run --project src/backend/ImpactoTextil.Api --launch-profile https
```

Esta comprobación solo consulta conectividad (`SELECT 1`); no crea tablas ni escribe en `master`. Para la aplicación de negocio se utilizará una base propia. `TrustServerCertificate=True` se limita a esta prueba local y no se copia a la configuración pública. Nunca guardar contraseñas o conexiones del proveedor en Git.

## Generar el paquete

```powershell
dotnet publish src/backend/ImpactoTextil.Api -c Release -o artifacts/publicacion --no-restore
```

Generar el paquete no lo publica. La compatibilidad del alojamiento, HTTPS público, configuración privada y persistencia de archivos se verifican siguiendo [la guía de despliegue](docs/fase-3/04-despliegue.md).

## Organización

| Ruta | Contenido |
| --- | --- |
| `docs/Impacto textil fase 1 y 2.docx` | Requisitos aprobados, sin cambios. |
| `docs/fase-3` | Modelo, 3FN, arquitectura, diagramas, decisiones y reparto. |
| `docs/seguimiento` | Evidencia de avances y pendientes. |
| `database` | Prototipo SQL y comprobaciones de integridad. |
| `src/backend/ImpactoTextil.Api` | API inicial. |
| `tests/ImpactoTextil.Api.Tests` | Tres pruebas HTTP de disponibilidad y errores. |
| `scripts` | Ensayo del modelo y generación de diagramas. |
| `artifacts` | Salidas locales ignoradas por Git. |

El frontend se incorporará bajo `src/frontend` al construir las pantallas; no se presenta una carpeta vacía como interfaz terminada.

## Próxima tarea de Yirardy

Cerrar con el equipo las decisiones del modelo y los contratos; convertir el modelo acordado en migración EF Core; construir **CU-01 completo** con pantalla, validación, consentimiento, BCrypt, persistencia y pruebas. Resolver antes la política de contraseña pendiente en el SRS. Continuar con CU-02, CU-09 y CU-04, integrando con las tareas de Dylan y Carlos.
