# CU-10: clases de diseño y correspondencia con el código

Fecha: 30 de octubre de 2026. Rama de elaboración: `Carlos_Borrell`. Responsable actual: Yirardy, continuando el avance de Carlos según [el reparto actualizado](../planes/REPARTO_ACTUAL.md).

Actualización del 31 de octubre: este diseño describe **Revisar y cotizar pedidos** con vista `pedidos.html`, controlador `PedidosAdministracionController`, servicios y Repository conectados. La situación anterior de frontera pendiente se conserva en el historial Git; el prototipo ficticio anterior sigue separado. Los contratos, comprobaciones y límites de la implementación actual están en [administración de pedidos](15-administracion-pedidos.md).

## Fuente y alcance

Los requisitos proceden exclusivamente de [Impacto textil fase 1 y 2.docx](../Impacto%20textil%20fase%201%20y%202.docx): CU-10, RF-06, RF-08, reglas de versiones y del anticipo del 50 %, y diccionarios de Pedido/DetallePedido e historial. La huella SHA-256 comprobada del DOCX es `9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`.

La correspondencia inicial se revisó sobre `e6569da` y se actualizó con la construcción del 31 de octubre. Las láminas muestran miembros seleccionados, sin reemplazar ni borrar los restantes del código o del diccionario. Los tipos mostrados son C#; no representan directamente los tipos SQL ni la forma serializada en JSON. No se modificó el esquema de datos.

## Archivos y uso

| Lámina | Contenido | Fuente Mermaid | Vista vectorial |
| --- | --- | --- | --- |
| Aplicación y persistencia | Vista, frontera pendiente, servicio, Repository, EF Core y resultado de operación | [Aplicación](diagramas/clases-cu10-aplicacion.mmd) | [SVG](diagramas/clases-cu10-aplicacion.svg) |
| DTO existentes | Entradas con validación y salidas de consulta, versiones y archivo | [DTO](diagramas/clases-cu10-dtos.mmd) | [SVG](diagramas/clases-cu10-dtos.svg) |
| Historial | Entidades, relaciones entre versiones y referencias vigentes, pagos como contexto de consulta y vista calculada | [Historial](diagramas/clases-cu10-historial.mmd) | [SVG](diagramas/clases-cu10-historial.svg) |

**Editar en draw.io:** abrir [Clases_CU10.drawio](diagramas/Clases_CU10.drawio) mediante Archivo → Abrir desde → Dispositivo. Contiene tres páginas, con cajas, miembros y conectores nativos editables. Los SVG sirven para insertar las figuras o consultarlas ampliadas. El archivo anterior `Impacto_Textil_Fase_3.drawio` conserva sus cinco páginas y no se regeneró en este trabajo.

**Usar Mermaid:** abrir el `.mmd` en VS Code y copiar su contenido en la opción de inserción Mermaid de draw.io. No copiar los tres archivos a la vez: cada uno es un diagrama independiente. El Mermaid y el draw.io comparten las clases y relaciones; su distribución puede variar.

**Regenerar los archivos nativos y SVG** desde la raíz del repositorio, con Python 3 sin dependencias:

```powershell
python scripts/Generar-Clases-CU10.py
```

El generador lee exclusivamente el subconjunto Mermaid empleado por estas tres fuentes y mantiene una distribución específica. Si se agregan clases o sintaxis distintas, exige ajustar el generador; no es un conversor Mermaid general. Las ediciones manuales del draw.io no regresan automáticamente al Mermaid y una regeneración las reemplaza: conservarlas mediante Git o trasladar primero los cambios a las fuentes/distribución.

## Cómo leer las clases

- `+` indica miembro público y `-` privado; `[static]` en el archivo nativo/SVG corresponde al sufijo `$` de Mermaid. `?` conserva la nulabilidad de C#. Los parámetros `...` se desarrollan en el inventario siguiente.
- `Task<T>` representa una operación asíncrona, `IReadOnlyList<T>` una colección de consulta y `ICollection<T>` una navegación de entidades. Las propiedades de entidades son públicas con `get/set`; los DTO de entrada tienen `get/init` y los de salida son records. La figura abrevia esos accesores.
- La línea discontinua con triángulo vacío significa realización de interfaz; con flecha abierta, dependencia. La asociación dirigida y la composición se distinguen por la flecha abierta y el rombo lleno, respectivamente. Las multiplicidades en los SVG/draw.io se leen como **extremo de origen → extremo de destino**; Mermaid las coloca junto a cada extremo.
- VistaGestionCU10 es el identificador del papel de presentación, no una clase C#. Sus funciones pertenecen al cierre de `pedidos.js`. PedidosAdministracionController sí es una clase C# implementada. Los miembros del controlador seleccionados corresponden a CU-10; también contiene las acciones de los otros CU administrativos descritas en el contrato.
- Los estereotipos explican el papel de cada elemento; no son anotaciones agregadas a las entidades. La configuración de EF Core está en Fluent API, dentro del DbContext.

## Correspondencia con archivos existentes

| Elemento | Archivo y responsabilidad |
| --- | --- |
| Vista conectada | [HTML](../../src/frontend/pedidos.html), [CSS](../../src/frontend/pedidos.css) y [JavaScript](../../src/frontend/pedidos.js): consulta y operaciones de la API con SQL. El prototipo anterior se conserva separado. |
| PedidosAdministracionController | [Controlador](../../src/backend/ImpactoTextil.Api/Controllers/PedidosAdministracionController.cs): identidad de sesión, permisos, DTO, archivos y traducción de resultados a HTTP. |
| SesionController | [Controlador existente](../../src/backend/ImpactoTextil.Api/Controllers/SesionController.cs): `Actual()` devuelve `ActionResult<SesionDto>` y permite consultar la sesión. |
| RevisionCotizacionAdminService | [Servicio](../../src/backend/ImpactoTextil.Api/Services/RevisionCotizacionAdminService.cs): autoriza al Administrador y coordina consulta, versiones, decisiones y archivo. Recibe `IPedidoAdministracionRepository` y `TimeProvider` por constructor. |
| IPedidoAdministracionRepository | [Interfaz](../../src/backend/ImpactoTextil.Api/Repositories/IPedidoAdministracionRepository.cs): acceso a datos y ejecución de una operación sobre el pedido bloqueado. |
| PedidoAdministracionRepository | [Implementación](../../src/backend/ImpactoTextil.Api/Repositories/PedidoAdministracionRepository.cs): usa `ImpactoTextilDbContext`, carga el expediente y guarda dentro de la transacción. Implementa también los miembros públicos de la interfaz, abreviados en la figura concreta. |
| ImpactoTextilDbContext | [Persistencia](../../src/backend/ImpactoTextil.Api/Infrastructure/ImpactoTextilDbContext.cs): DbSet, claves, relaciones y configuración EF. Solo se dibujan los conjuntos relevantes para este recorrido. |
| DTO y validación | [AdministracionDtos.cs](../../src/backend/ImpactoTextil.Api/Contracts/AdministracionDtos.cs): entradas existentes y records de salida. |
| ReglasAdministrativasPedido | [Reglas](../../src/backend/ImpactoTextil.Api/Services/ReglasAdministrativasPedido.cs): cálculos y condiciones del archivo administrativo; la figura no muestra sus métodos de producción/entrega. |
| ResultadoOperacionAdmin y ErrorOperacionAdmin | [Resultado de operación](../../src/backend/ImpactoTextil.Api/Services/ResultadoOperacionAdmin.cs): valor o error de negocio tipado. El controlador traduce el resultado a HTTP. |
| Entidades e historial | [Pedido](../../src/backend/ImpactoTextil.Api/Domain/Entities/Pedido.cs), [DetallePedido](../../src/backend/ImpactoTextil.Api/Domain/Entities/DetallePedido.cs), [VersionCotizacion](../../src/backend/ImpactoTextil.Api/Domain/Entities/VersionCotizacion.cs), [DetalleCotizacion](../../src/backend/ImpactoTextil.Api/Domain/Entities/DetalleCotizacion.cs) y [VersionDiseno](../../src/backend/ImpactoTextil.Api/Domain/Entities/VersionDiseno.cs). |
| Contexto y proyección | [Usuario](../../src/backend/ImpactoTextil.Api/Domain/Entities/Usuario.cs), [Pago](../../src/backend/ImpactoTextil.Api/Domain/Entities/Pago.cs) y [vPedido](../../src/backend/ImpactoTextil.Api/Domain/Entities/vPedido.cs). |

Las entradas implementan `IValidatableObject.Validate(ValidationContext)`; los records de salida no ejecutan ese método. `RegistrarVersionDisenoTextoDto` contiene la descripción, pero no contiene un archivo ni determina un formato HTTP de subida. Los DTO referenciados por el expediente y no expandidos en la lámina —DetallePedidoAdminDto, VersionCotizacionAdminDto y VersionDisenoAdminDto, entre otros— permanecen definidos en el mismo archivo de contratos.

Los importes son `decimal`/`decimal?` en C#, con serialización JSON como cadenas decimales mediante `JsonNumberHandling`; no deben convertirse a tipos binarios de punto flotante para operar dinero. Los identificadores y cantidades continúan siendo enteros.

## Inventario de operaciones públicas de CU-10

Firmas existentes de `RevisionCotizacionAdminService`, sin los cuerpos. `async` es un detalle de implementación y se omite en este inventario; los retornos conservan `Task` y los métodos reciben cancelación.

```csharp
public Task<ResultadoOperacionAdmin<IReadOnlyList<ResumenPedidoAdminDto>>> ListarPedidosAsync(
    int administradorId,
    FiltroPedidosAdminDto filtro,
    CancellationToken cancelacion);

public Task<ResultadoOperacionAdmin<ExpedientePedidoAdminDto>> ObtenerExpedienteAsync(
    int administradorId,
    int pedidoId,
    CancellationToken cancelacion);

public Task<ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>> IncorporarVersionDisenoAsync(
    int administradorId,
    int pedidoId,
    int detalleId,
    string? archivoRelativo,
    string? descripcion,
    CancellationToken cancelacion);

public Task<ResultadoOperacionAdmin<ResultadoVersionDisenoAdminDto>> DecidirVersionDisenoAsync(
    int administradorId,
    int pedidoId,
    int detalleId,
    int versionDisenoId,
    DecidirVersionDisenoDto solicitud,
    CancellationToken cancelacion);

public Task<ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>> ProponerCotizacionAsync(
    int administradorId,
    int pedidoId,
    ProponerCotizacionDto solicitud,
    CancellationToken cancelacion);

public Task<ResultadoOperacionAdmin<ResultadoCotizacionAdminDto>> DecidirCotizacionAsync(
    int administradorId,
    int pedidoId,
    int versionCotizacionId,
    DecidirCotizacionDto solicitud,
    CancellationToken cancelacion);

public Task<ResultadoOperacionAdmin<ResultadoArchivoPedidoDto>> ArchivarSolicitudAsync(
    int administradorId,
    int pedidoId,
    ArchivarPedidoDto solicitud,
    CancellationToken cancelacion);
```

Operaciones de `IPedidoAdministracionRepository` utilizadas por este servicio:

```csharp
Task<Usuario?> ObtenerAdministradorActivoAsync(int usuarioId, CancellationToken cancelacion);

Task<IReadOnlyList<ResumenPedidoAdminDto>> ListarPedidosAsync(
    string? estado,
    string condicionAdministrativa,
    CancellationToken cancelacion);

Task<ExpedientePedidoAdminDto?> ObtenerExpedienteAsync(
    int pedidoId,
    CancellationToken cancelacion);

Task<ResultadoOperacionAdmin<T>> EjecutarEnPedidoBloqueadoAsync<T>(
    int pedidoId,
    Func<Pedido, Func<CancellationToken, Task>, CancellationToken, Task<ResultadoOperacionAdmin<T>>> operacion,
    CancellationToken cancelacion);
```

El callback recibe el pedido cargado, una función para guardar un paso intermedio sin cerrar la transacción y el token de cancelación. Esto permite registrar la decisión de una versión antes de asignarla como vigente, respetando las restricciones existentes. El repositorio administra el commit/rollback de toda la operación. Sus dos consultas de referencia de archivo pertenecen al recorrido de CU-11 y se conservan en el código; no se expanden en esta lámina de CU-10.

## Recorrido para explicar y defender el diseño

1. **Vista:** consulta la sesión y los pedidos de la API; sus formularios quedan vinculados al identificador del expediente mostrado. El prototipo anterior continúa usando memoria y no se confunde con esta pantalla.
2. **Controlador:** obtiene la identidad desde la sesión, exige Administrador, valida DTO y protege las escrituras contra CSRF. Las rutas y respuestas están documentadas en el contrato administrativo; su integración con los módulos de Dylan se revisará posteriormente.
3. **Servicio existente:** vuelve a consultar que el Administrador esté activo; verifica pertenencia del detalle/versión al pedido, decisiones permitidas, cobertura de la cotización y condiciones del archivo. Devuelve un resultado de negocio sin depender de una ruta HTTP.
4. **Repositorio existente:** ejecuta la modificación con bloqueo de la fila del pedido y transacción; el expediente también se lee bajo una transacción Serializable y bloqueo del padre. Las pruebas HTTP/SQL actuales incluyen administradores y cobros simultáneos.
5. **Persistencia:** EF Core usa el modelo relacional existente. SQL conserva versiones, responsables y decisiones; la elección de una versión vigente no reemplaza los registros anteriores. Este trabajo de documentación no cambia migraciones ni el esquema.
6. **Archivos:** `IncorporarVersionDisenoAsync` recibe una referencia privada generada por el servidor y/o descripción. El controlador coordina validación/almacenamiento y limpieza condicionada a que no haya referencia confirmada. No admite una ruta arbitraria del navegador como referencia ya validada.

## Relaciones e invariantes relevantes

| Relación o cálculo | Interpretación y límite |
| --- | --- |
| Pedido y DetallePedido | Una solicitud válida tiene uno o más detalles; cada detalle pertenece a un pedido. Las multiplicidades mínimas expresan el pedido válido completo, no que una FK por sí sola garantice una colección no vacía. |
| Historial de cotización y de diseño | Un pedido puede tener cero o más cotizaciones y un detalle cero o más versiones de diseño. Cada versión tiene un padre obligatorio. Su contenido y su decisión definitiva no se sobrescriben. Las composiciones expresan pertenencia del ciclo de vida, **no borrado físico en cascada** del historial. |
| Referencias vigentes | Un pedido/detalle puede no tener versión vigente; una versión histórica puede no ser vigente para ninguno. Por eso ambas asociaciones de elección muestran `0..1` en ambos extremos. Solo puede elegirse una versión del propio padre, Aceptada/Aprobada según corresponda. |
| DetalleCotizacion | Cada línea conserva el precio acordado y referencia un detalle original; la cantidad original permanece inmutable para recuperar todas las cotizaciones. Una cotización válida debe cubrir todos los detalles. Puede referenciar un diseño cotizado de ese mismo detalle. |
| Usuario | Las flechas dibujan al responsable del registro. Los responsables de decisión también existen en los campos y relaciones del modelo, aunque sus asociaciones no se expanden para mantener legible la lámina. |
| vPedido | Es una vista de consulta sin clave EF, no una entidad a insertar. Total referencial, total vigente, anticipo y saldo son proyecciones/cálculos; no se agregan columnas redundantes de esos importes a Pedido. |
| Pagos | Se leen como contexto del expediente y para sumar solamente Verificados. Pendiente/Rechazado no acreditan dinero. La figura no implementa CU-07 ni CU-13. |
| Anticipo | `CalcularAnticipoExigible` exige el 50 % del total aceptado, redondeado hacia arriba al centavo. Para C$100.01 exige C$50.01. Una propuesta aún no aceptada no cambia el acuerdo vigente. |
| Aumento durante producción | Al aceptar total C$1,400 con C$500 verificados, el nuevo mínimo es C$700 y faltan C$200. Se conservan pagos y estado; la continuación de sublimación depende de la revalidación posterior en CU-12 y del diseño vigente aprobado. La aceptación sola no la autoriza. |
| Archivo administrativo | El servicio exige condición Activa, estado Pendiente, ausencia de cotizaciones aceptadas y ausencia de pagos Verificados o Pendientes. Registra motivo/fecha/responsable y conserva historial. Archivada es una condición administrativa; no añade un quinto estado productivo. |

## Comprobaciones y pendientes

- Los tres fuentes se analizaron correctamente con Mermaid 12.2.0, instalado solo en el entorno temporal de comprobación, sin dependencia nueva para la aplicación.
- Se generaron tres SVG y un XML draw.io de tres páginas con 30 cajas y 24 relaciones en total; se verificaron identificadores, referencias de conectores y límites de página. Se revisaron las tres láminas renderizadas localmente para corregir textos/flechas. El XML no se abrió en la interfaz de draw.io; su estructura y elementos nativos se comprobaron de forma local.
- La revisión independiente de código y diagrama identificó y permitió corregir las multiplicidades de elección de versión vigente. No sustituye la aprobación del compañero ni del docente.
- La huella del DOCX fuente se conserva. El diseño inicial no ejecutaba pruebas funcionales; la construcción posterior sí incluye SQL y pagos, con evidencia separada en el documento 15. Main, despliegue e integración con los módulos de Dylan siguen pendientes.

La implementación local actual está descrita en el documento 15. Siguen pendientes la revisión humana y la integración con el recorrido del cliente de Dylan. Estas láminas cubren el diseño seleccionado de CU-10, no todos los diagramas de clases del proyecto.
