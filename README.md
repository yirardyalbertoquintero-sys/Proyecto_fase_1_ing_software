Proyecto ing software

# Impacto Textil — fases 3 y 4, rama Carlos_Borrell

Aplicación de pedidos de camisetas personalizadas por sublimación. Este avance prepara la parte inicial de **Yirardy**: diseño de datos, normalización, arquitectura, entorno común y comprobación técnica de la API.

**Carpeta única de trabajo:** `C:\proyectos\Proyecto_fase_1_ing_software`. Esta rama es `Carlos_Borrell`; la base de acceso/catálogo procede de `Yirardy_Quintero`. Abre la carpeta completa en VS Code y comprueba la rama antes de editar. La carpeta adicional `Impacto_Textil_Yirardy` ya no se utiliza.

**Actualización del 31 de octubre de 2026:** el bloque administrativo CU-10/CU-11/CU-13/CU-12 está conectado a pantalla, API y SQL Server. Desde Mi sesión o Gestionar catálogo, abrir **Gestionar pedidos**. Incluye cotizaciones y diseños con historial, descargas privadas, comprobantes/cobros y producción/entrega. La suite actual, incluida la nueva política de contraseñas, pasó 92 pruebas sin omisiones. La revisión humana y la integración del recorrido del cliente de Dylan siguen pendientes. [Cómo revisar, contratos y evidencias](docs/fase-3/15-administracion-pedidos.md).

**Antecedente del 30 de octubre de 2026:** se reunieron las historias de Carlos y Yirardy conservando los commits anteriores, y se corrigieron servicios, almacenamiento privado y DTO monetarios. En ese cierre todavía faltaba la conexión administrativa, completada en la actualización del 31. Ver [correcciones y comprobaciones de ese cierre](docs/seguimiento/correcciones-carlos-2026-10-30.md).

**Estado al 29 de octubre de 2026:** CU-01, CU-02, CU-04 y CU-09 implementados y probados localmente: registro, acceso/cierre, permisos, catálogo público y administración de variantes con fotos y baja lógica. La sesión vence tras 10 minutos de inactividad. Hay un comando controlado para crear el primer Administrador. Pedidos, pagos, producción y alojamiento en MonsterASP.NET siguen pendientes. Los hitos están en [CU-01](docs/fase-3/08-registro-clientes.md), [CU-02](docs/fase-3/09-acceso-sesiones.md), [CU-09](docs/fase-3/10-catalogo-administracion.md) y [CU-04](docs/fase-3/11-catalogo-publico.md). La revisión de Dylan y la integración en main siguen pendientes.

## Abrir el registro

**Desde VS Code:** abre esta carpeta, entra en **Ejecutar y depurar**, selecciona **Impacto Textil: ejecutar aplicación** y presiona **F5**. El formulario se abrirá automáticamente al arrancar el servidor. [Instrucciones de VS Code](docs/ejecutar-en-vscode.md).

Desde PowerShell, en la raíz de esta carpeta:

```powershell
.\scripts\Iniciar-Local.ps1
```

El comando restaura las herramientas, aplica las migraciones a la base propia `ImpactoTextil` e inicia la aplicación. Abre **https://localhost:7105/**. Deja la consola abierta y usa Ctrl+C para detenerla. Si ya existe una instancia ejecutándose en ese puerto, detenla antes de iniciar otra. Para una instancia Express, pasa `-Servidor '.\SQLEXPRESS'`.

Usa datos ficticios: el aviso incluido describe exclusivamente la demostración académica. Después del registro pulsa **Inicia sesión para entrar a tu cuenta**; también está el enlace **Inicia sesión** debajo del formulario. El acceso muestra tu identidad y permite cerrar la sesión. El registro no inicia sesión automáticamente. [Registro](docs/fase-3/08-registro-clientes.md) · [Acceso, contratos y pruebas](docs/fase-3/09-acceso-sesiones.md).

## Probar la administración del catálogo

1. Detén la ejecución anterior con **Shift+F5** para que VS Code compile la versión nueva.
2. Si todavía no existe Administrador, abre **Terminal → Ejecutar tarea → Impacto Textil: crear primer Administrador**. Escribe nombre, correo y contraseña en esa terminal; la contraseña no se muestra. También puedes ejecutar `./scripts/Crear-Administrador.ps1` desde la raíz. La base debe estar preparada mediante F5 o `Iniciar-Local.ps1 -SoloPreparar`.
3. Ejecuta de nuevo con **F5**, pulsa **Inicia sesión** y entra con esa cuenta. Aparecerá **Gestionar catálogo**.

Puedes crear variantes, consultar y filtrar el listado, cambiar datos/precio/foto y desactivar la disponibilidad. No se elimina físicamente ningún producto. No hay cuentas ni contraseñas predeterminadas. El catálogo empieza vacío; las fotos y productos de las evidencias se generaron solo en bases temporales.

## Consultar el catálogo público

Ejecuta con **F5** y pulsa **Explorar el catálogo de prendas**, desde el registro o la pantalla de sesión. También aparece **Ver catálogo** en la administración. No necesitas cuenta para consultar. Si la versión anterior está abierta, detenla con **Shift+F5** antes de volver a ejecutar.

Se muestran las variantes activas y sus precios de referencia. Puedes buscar, filtrar por talla/color/tela y pulsar **Ver prenda** para consultar sus características. El catálogo se alimenta de lo guardado en administración; si todavía no hay variantes activas, se informa sin inventar productos. La configuración de prendas y el envío de solicitudes corresponden a CU-05/CU-06 y aún no están implementados.

## Prototipo de revisión de pedidos

El prototipo anterior sigue disponible en **Gestionar catálogo → Pedidos · prototipo**, con acciones ficticias en memoria que se reinician al recargar. Se conserva como antecedente: [alcance y capturas](docs/fase-3/13-prototipo-revision-pedidos.md). Para la implementación con persistencia, abrir **Gestionar pedidos**.

El [diseño de clases de CU-10](docs/fase-3/14-diseno-clases-cu10.md) explica tipos, operaciones, Repository/DTO e historial. Incluye [tres páginas editables en draw.io](docs/fase-3/diagramas/Clases_CU10.drawio) y fuentes Mermaid, actualizadas con el controlador implementado. Las pruebas se documentan por separado del diseño.

## Revisar los pedidos administrativos

Con F5 y una sesión de Administrador, abrir **Gestionar pedidos**. Si la base está vacía, detener la aplicación y ejecutar **Terminal → Ejecutar tarea → Impacto Textil: crear pedidos de demostracion**. Confirmar `DEMO` en esa terminal y volver a ejecutar con F5. Se agregan tres pedidos ficticios a la base local; repetir la tarea conserva las decisiones existentes. Requiere haber preparado la base y creado primero el Administrador. [Recorridos de evaluación y límites](docs/fase-3/15-administracion-pedidos.md).

## Preparar publicación

En **Terminal → Ejecutar tarea**, seleccionar **Impacto Textil: preparar publicacion**, o ejecutar `./scripts/Preparar-Publicacion.ps1`. Requiere SQL Server local y cambios de construcción guardados en Git. El resultado queda en una carpeta nueva de `artifacts/entregas`, con sitio.zip, SQL separado, manifiesto, pruebas y huellas. Una carpeta con NO-PUBLICAR.txt no es una entrega terminada.

El ensayo de la aplicación publicada en modo Production local pasó las comprobaciones de lectura y persistencia tras reinicio. MonsterASP.NET sigue pendiente: todavía no hay cuenta del proyecto. [Guía de instalación](docs/despliegue/INSTALAR-MONSTERASP.md) · [Evidencia y límites](docs/fase-3/12-preparacion-publicacion.md).

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

La suite contiene 92 pruebas: 51 de la base, 18 de archivos privados/serialización, 14 del bloque administrativo y 9 añadidas al actualizar la política de contraseñas. La ejecución posterior al cambio pasó las 92 con SQL Server real, sin fallos ni omisiones. Para repetir la suite:

```powershell
.\scripts\Test-Registro.ps1
```

Sin configurar SQL, las 54 pruebas de integración se omiten. Una de ellas también ejecuta las 29 comprobaciones del modelo sobre una base creada por la migración; no son 29 pruebas xUnit adicionales. Cada ejecución retira únicamente su propia base temporal; no borra `ImpactoTextil`. El informe histórico de correcciones conserva su comando filtrado de archivos/serialización.

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

## Próximo paso del bloque administrativo

Yirardy asumió y autorizó completar CU-10/CU-11/CU-13/CU-12 en esta rama, con contratos y pruebas funcionales administrativas. El bloque local está conectado; el siguiente hito es su revisión humana y posterior coordinación con las solicitudes, comprobantes y consulta de Dylan. Main, alojamiento e informe final continúan aplazados. La política actualizada el 31 de octubre exige contraseña nueva de al menos 8 caracteres, con mayúscula, minúscula, número y símbolo; máximo 72 bytes UTF-8 sin recorte. La sesión conserva 10 minutos de inactividad y las cuentas anteriores siguen pudiendo entrar. Ver [cambio y comprobaciones](docs/seguimiento/politica-contrasena-2026-10-31.md).
