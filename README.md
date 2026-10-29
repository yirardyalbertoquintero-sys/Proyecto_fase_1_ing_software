Proyecto ing software

# Impacto Textil — inicio de las fases 3 y 4

Aplicación de pedidos de camisetas personalizadas por sublimación. Este avance prepara la parte inicial de **Yirardy**: diseño de datos, normalización, arquitectura, entorno común y comprobación técnica de la API.

**Carpeta única de trabajo:** `C:\proyectos\Proyecto_fase_1_ing_software`, rama `Yirardy_Quintero`. Abre esta carpeta completa en VS Code. La carpeta adicional `Impacto_Textil_Yirardy` ya no se utiliza.

**Estado al 29 de octubre de 2026:** CU-01 y CU-02 implementados y probados localmente: registro, inicio y cierre de sesión, BCrypt, consentimiento, duplicados y permisos. La sesión vence tras 10 minutos de inactividad, conforme a lo confirmado por Yirardy. Las entidades, restricciones y migración inicial EF Core están incorporadas. Catálogo, pedidos, pagos y alojamiento en MonsterASP.NET siguen pendientes. El hito anterior publicado está en el [registro de publicación](docs/seguimiento/publicacion-2026-10-29.md); el nuevo avance de acceso se documenta en [CU-02](docs/fase-3/09-acceso-sesiones.md).

## Abrir el registro

**Desde VS Code:** abre esta carpeta, entra en **Ejecutar y depurar**, selecciona **Impacto Textil: ejecutar aplicación** y presiona **F5**. El formulario se abrirá automáticamente al arrancar el servidor. [Instrucciones de VS Code](docs/ejecutar-en-vscode.md).

Desde PowerShell, en la raíz de esta carpeta:

```powershell
.\scripts\Iniciar-Local.ps1
```

El comando restaura las herramientas, aplica las migraciones a la base propia `ImpactoTextil` e inicia la aplicación. Abre **https://localhost:7105/**. Deja la consola abierta y usa Ctrl+C para detenerla. Si ya existe una instancia ejecutándose en ese puerto, detenla antes de iniciar otra. Para una instancia Express, pasa `-Servidor '.\SQLEXPRESS'`.

Usa datos ficticios: el aviso incluido describe exclusivamente la demostración académica. Después del registro pulsa **Inicia sesión para entrar a tu cuenta**; también está el enlace **Inicia sesión** debajo del formulario. El acceso muestra tu identidad y permite cerrar la sesión. El registro no inicia sesión automáticamente. [Registro](docs/fase-3/08-registro-clientes.md) · [Acceso, contratos y pruebas](docs/fase-3/09-acceso-sesiones.md).

## Empezar por aquí

1. Leer [la fuente vigente](docs/FUENTE_VIGENTE.md) y `AGENTS.md`. El DOCX aprobado se conserva intacto. Los archivos anteriores del repositorio no sustituyen sus requisitos.
   El [índice de documentos](docs/INDICE.md) distingue requisitos, guías, plan, diseño, evidencias y archivo histórico.
2. Revisar [el diseño de fase 3](docs/fase-3/00-inicio.md), el [flujo del equipo](docs/fase-3/07-flujo-equipo.md) y el [avance comprobado](docs/seguimiento/avance-yirardy.md).
3. Abrir [las cinco páginas editables](docs/fase-3/diagramas/Impacto_Textil_Fase_3.drawio) en draw.io. Las versiones SVG y Mermaid están en la misma carpeta.
4. Ejecutar las comprobaciones siguientes desde la raíz del repositorio.

## Herramientas necesarias

- SDK .NET 10.0.402, fijado en `global.json` con actualización de parches permitida.
- SQL Server y `sqlcmd` para ensayar el modelo. Las pruebas rápidas se ejecutan sin SQL Server; la suite de integración utiliza una base temporal real.
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

Para ejecutar **las 20 pruebas**, incluidas las 11 de integración con SQL Server:

```powershell
.\scripts\Test-Registro.ps1
```

Sin ese script, las 11 pruebas que requieren SQL se muestran como omitidas. Una prueba de integración también ejecuta las 29 comprobaciones del modelo sobre una base creada por la migración. Cada ejecución retira únicamente su propia base temporal; no borra `ImpactoTextil`.

Los archivos `packages.lock.json` fijan también las dependencias transitivas. Si se cambia intencionalmente una dependencia, regenerar el bloqueo, revisar el cambio y repetir las comprobaciones. `Directory.Build.props` trata las alertas de vulnerabilidad moderada, alta o crítica como errores de restauración.

## Ensayar el modelo de datos

```powershell
./scripts/Test-Modelo.ps1 -Servidor localhost
```

El usuario de Windows necesita permiso para crear una base de prueba. Para Express, usar el nombre real de su instancia, por ejemplo `-Servidor '.\SQLEXPRESS'`. El script crea una base temporal con nombre único, aplica el modelo, ejecuta 29 comprobaciones y elimina únicamente esa base al finalizar. Usa datos ficticios; no sirve para cargar datos de producción.

`database/001_modelo_fase3.sql` se conserva como antecedente del **prototipo de diseño**. La autoridad actual está en `src/backend/ImpactoTextil.Api/Migrations`, incluido su snapshot y los complementos de vistas y disparadores. `database/002_migracion_inicial.sql` es la distribución idempotente generada desde esa migración. No combinar ambos scripts sobre una misma base ni editar el archivo generado como si fuera una segunda autoridad.

## Ejecutar la comprobación de disponibilidad

```powershell
.\scripts\Iniciar-Local.ps1
```

La ruta técnica es `https://localhost:7105/api/disponibilidad`; el formulario está en `/`. Sin conexión configurada la comprobación técnica responde **503**. Si falta el certificado de desarrollo, generarlo con `dotnet dev-certs https`; la confianza del certificado local se gestiona en cada equipo.

Si la base ya está preparada y prefieres iniciar manualmente, define la conexión **en la misma consola** desde la que ejecutas la aplicación:

```powershell
$env:ConnectionStrings__ImpactoTextil = 'Server=localhost;Database=ImpactoTextil;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5'
dotnet run --project src/backend/ImpactoTextil.Api --launch-profile https
```

El registro utiliza la base propia `ImpactoTextil`. `master` solo se utilizó en la comprobación inicial de conectividad. `TrustServerCertificate=True` se limita al desarrollo local y no se copia a la configuración pública. Nunca guardar contraseñas o conexiones del proveedor en Git.

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
| `src/backend/ImpactoTextil.Api` | API, entidades, Repository, servicio, DTO y migraciones. |
| `src/frontend` | Formulario de registro y aviso de la demostración; HTML, CSS y JavaScript puro. |
| `tests/ImpactoTextil.Api.Tests` | Disponibilidad, validación, BCrypt, integración HTTP/SQL y migración. |
| `scripts` | Ensayo del modelo y generación de diagramas. |
| `artifacts` | Salidas locales ignoradas por Git. |

El frontend se copia a `wwwroot` de la compilación y del paquete de publicación. La API lo sirve desde el mismo origen. No necesita npm ni un framework de JavaScript.

## Próxima tarea de Yirardy

Revisar CU-01 con el equipo y continuar con **CU-02: inicio y cierre de sesión**, acordando la duración de sesión pendiente en el SRS. Después CU-09 y CU-04. La contraseña mínima de 15 caracteres fue confirmada por Yirardy en esta conversación el 28 de octubre de 2026; el límite de BCrypt se valida sin recorte silencioso.
