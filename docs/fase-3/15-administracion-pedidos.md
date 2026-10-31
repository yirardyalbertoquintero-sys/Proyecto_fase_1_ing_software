# Pedidos administrativos conectados a SQL Server

Fecha de construcción: 31 de octubre de 2026. Rama: Carlos_Borrell. Yirardy autorizó completar CU-10, CU-11, CU-13 y CU-12 sin esperar la publicación de Dylan. La autorización incluye contratos administrativos y pruebas funcionales de pagos; main y despliegue continúan aplazados. La revisión humana la realizará el líder después de esta entrega.

La pantalla **Gestionar pedidos**, en `pedidos.html`, utiliza la API y SQL Server reales. `prototipo-pedidos.html` se conserva como antecedente con sus datos simulados en memoria. La fuente comercial permanece intacta: [DOCX vigente](../Impacto%20textil%20fase%201%20y%202.docx), SHA-256 `9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`.

## Qué se construyó

- **CU-10 / RF-06 y RF-08:** listado por estado y condición administrativa, búsqueda local por código/cliente, expediente, precios y versiones de cotización, decisiones con referencia y fecha acordada, versiones descritas o gráficas de diseño, aprobación/rechazo e historial. El archivo administrativo conserva el estado y las versiones.
- **CU-11 / RF-09:** descarga privada del original y de versiones de diseño, con pertenencia Pedido/Detalle/Versión y autorización de Administrador.
- **CU-13 / RF-07 y RF-11:** consulta privada de comprobantes, verificación o rechazo del mismo Pago Pendiente, cobro directo nuevo con confirmación idempotente, importes/saldo recalculados y protección ante operaciones simultáneas.
- **CU-12 / RF-10 y RF-11:** secuencia de producción, comprobación explícita de sublimación para versiones vigentes, revalidación del anticipo, entrega con confirmación y fecha real. Para la demostración se conserva saldo cero; la política comercial de entrega real sigue pendiente de validación según SRS 5.5.

El controlador `PedidosAdministracionController` obtiene la identidad de la sesión; el navegador no suministra el responsable. Program.cs registra servicios, Repository y almacenamiento privado. No se añadieron tablas, migraciones ni paquetes: se utiliza la migración inicial vigente. Las incorporaciones de Dylan deberán integrarse contra estas mismas entidades y revisarse con él; esta entrega no implementa CU-05/06/07/08.

El [diseño de CU-10](14-diseno-clases-cu10.md) incluye sus tres páginas editables. Las operaciones de descarga, pagos y producción se amplían en el [diagrama de clases administrativo](diagramas/clases-administracion-operaciones.mmd), con [versión SVG](diagramas/clases-administracion-operaciones.svg). Esta figura es amplia y permite inspección vectorial ampliada; la fuente Mermaid puede insertarse y redistribuirse en draw.io. Muestra miembros seleccionados de clases C# existentes, sin añadir entidades al negocio.

## Ejecutar y revisar en VS Code

1. Abrir la única carpeta activa, `C:\proyectos\Proyecto_fase_1_ing_software`, y comprobar `Carlos_Borrell`.
2. Detener la instancia anterior con Shift+F5. Ejecutar **Impacto Textil: ejecutar aplicación** con F5 para preparar la base y compilar el frontend actualizado.
3. Si aún no existe Administrador, detener la aplicación y ejecutar **Terminal → Ejecutar tarea → Impacto Textil: crear primer Administrador**. El comando solicita datos en la terminal, con contraseña mínima de 15 caracteres y máximo de 72 bytes UTF-8. No usa credenciales predeterminadas ni convierte Clientes. Volver a ejecutar con F5.
4. Iniciar sesión como Administrador y abrir **Gestionar pedidos**, desde Mi sesión o desde Gestionar catálogo.
5. En una base sin solicitudes se muestra una lista vacía. Para revisar sin el trabajo de Dylan, usar expresamente la herramienta local de datos ficticios descrita debajo.

### Datos ficticios para la revisión del líder

Con la base preparada y el Administrador creado, detener la aplicación y ejecutar la tarea **Impacto Textil: crear pedidos de demostracion**. También puede ejecutarse desde la raíz:

```powershell
.\scripts\Crear-PedidosDemostracion.ps1
```

Solicita escribir `DEMO` para agregar hasta tres pedidos a la base local `ImpactoTextil`; admite `-Servidor '.\SQLEXPRESS'` si esa es la instancia real. Luego arrancar con F5.

| Código | Recorrido que permite revisar |
| --- | --- |
| DEMO-ADMIN-01 | Propuesta inicial y diseño pendiente, descarga, aceptación, nuevas versiones y rechazos. |
| DEMO-ADMIN-02 | Total aceptado C$1,000, diseño aprobado y comprobante Pendiente de C$500. Verificarlo, iniciar producción, proponer C$700 por cada una de las dos prendas para alcanzar C$1,400 y observar el faltante de C$200. |
| DEMO-ADMIN-03 | Solicitud sin cotización aceptada, para probar archivo administrativo y conservación del historial. |

La herramienta conserva los códigos ya existentes y sus decisiones; repetirla no reinicia la demostración. Crea un Cliente ficticio inactivo sin contraseña compartida y una variante ficticia inactiva; esa variante no aparece en el catálogo público. Los archivos son muestras ficticias, no comprobantes de dinero recibido. No ejecuta flujos del cliente ni acredita la implementación de Dylan. La opción de consola se rechaza fuera de Development y no existe endpoint para sembrar datos. Si falla un commit, los archivos privados se conservan para revisión; no se borran archivos que puedan haber quedado referenciados.

## Contrato de la API administrativa

Prefijo: `/api/administracion/pedidos`. Todas las operaciones requieren sesión de Administrador activo. Las escrituras requieren el token de `/api/seguridad/token` en `X-CSRF-TOKEN`, con sus cookies del mismo origen. Ninguna operación se ofrece como GET para modificar datos. El contenido JSON desconocido se rechaza mediante los DTO existentes.

| Método y sufijo | Entrada | Salida al confirmar |
| --- | --- | --- |
| GET raíz | Query `estado` opcional, `condicionAdministrativa` Activa/Archivada/Todas; Activa por defecto | Lista de ResumenPedidoAdminDto |
| GET `/{pedidoId}` | Identificador positivo | ExpedientePedidoAdminDto |
| POST `/{pedidoId}/cotizaciones` | ProponerCotizacionDto: líneas de todos los detalles, precios y asociación opcional a diseño propio | ResultadoCotizacionAdminDto |
| POST `/{pedidoId}/cotizaciones/{versionId}/decision` | DecidirCotizacionDto: decisión, referencia, fecha acordada al aceptar | ResultadoCotizacionAdminDto |
| POST `/{pedidoId}/detalles/{detalleId}/disenos/texto` | RegistrarVersionDisenoTextoDto | ResultadoVersionDisenoAdminDto |
| POST `/{pedidoId}/detalles/{detalleId}/disenos/archivo` | Multipart: archivo obligatorio, descripcion opcional | ResultadoVersionDisenoAdminDto |
| POST `/{pedidoId}/detalles/{detalleId}/disenos/{versionId}/decision` | DecidirVersionDisenoDto | ResultadoVersionDisenoAdminDto |
| POST `/{pedidoId}/archivo` | ArchivarPedidoDto | ResultadoArchivoPedidoDto |
| GET `/{pedidoId}/detalles/{detalleId}/diseno-original` | Identificadores del pedido/detalle | Archivo descargable |
| GET `/{pedidoId}/detalles/{detalleId}/disenos/{versionId}/archivo` | Identificadores del pedido/detalle/versión | Archivo descargable |
| GET `/{pedidoId}/pagos/{pagoId}/comprobante` | Identificadores del pedido/pago | Archivo descargable |
| POST `/{pedidoId}/pagos/{pagoId}/decision` | ResolverPagoPendienteDto | ResultadoPagoAdminDto |
| POST `/{pedidoId}/cobros` | RegistrarCobroDirectoDto: monto, fecha, medio, referencia y GUID confirmacion | ResultadoPagoAdminDto |
| POST `/{pedidoId}/produccion` | ActualizarEstadoProduccionDto | ResultadoEstadoProduccionDto |
| POST `/{pedidoId}/sublimacion` | Sin datos de negocio; la interfaz envía `{}` | ResultadoAutorizacionSublimacionDto |

Las confirmaciones devuelven 200; `esCreacionNueva` diferencia creación de cobro de reintento. Los importes C# decimal viajan como cadenas JSON, conservando centavos. Identificadores y cantidades son enteros. Las descargas usan Content-Disposition attachment, Cache-Control no-store y nosniff, sin incluir rutas físicas en los DTO.

Errores: 400 para entradas inválidas o CSRF, 401 sin sesión válida, 403 por permisos, 404 para registros/archivos inexistentes, 409 ante un estado incompatible, decisión diferente o conflicto de concurrencia. Los límites HTTP pueden responder 413 antes del controlador. Errores inesperados se traducen al ProblemDetails general, con referencia de diagnóstico y sin excepción cruda.

Los cuerpos JSON administrativos pequeños tienen límite técnico de 16 KiB; la propuesta completa admite 128 KiB. La petición multipart admite 10,050,000 bytes para permitir metadatos alrededor del archivo, cuyo máximo individual sigue siendo inclusivo de 10,000,000 bytes. Los diseños son JPG/PNG; comprobantes JPG/PNG/PDF. Se conserva la validación completa existente y el presupuesto técnico de 12 millones de píxeles, documentado en la corrección anterior. No se acepta una ruta de archivo aportada por el navegador.

## Integridad y decisiones

- Las escrituras bloquean la fila del pedido y cargan sus colecciones dentro de una transacción. Un resultado fallido revierte incluso los pasos intermedios guardados. El expediente se consulta de forma consistente bajo Serializable y bloqueo del padre.
- La repetición de una decisión solo se acepta si coincide con los datos ya registrados; una referencia/fecha incompatible da conflicto y nunca reescribe el historial. Un cobro directo conserva su GUID cuando se pierde la respuesta, para que el reintento no acredite dinero dos veces.
- La verificación abre y valida el comprobante privado del mismo Pago antes de resolverlo. La suma usa únicamente Verificados. Un rechazo conserva responsable/fecha y no acredita dinero. No hay pasarela ni comprobación bancaria automatizada.
- La subida registra únicamente una referencia generada por el servidor. Ante error, comprueba que el archivo no esté referenciado antes de retirarlo. Si la comprobación no puede completarse, conserva el archivo y registra el diagnóstico para revisión; esto evita borrar un diseño confirmado tras una respuesta perdida.
- Sin personalización se documenta mediante una versión descrita y su constancia aprobada; no exige imagen. Tela, color, cantidades y precios históricos originales se conservan.
- La condición Archivada es independiente de los cuatro estados. El archivo no procede con un acuerdo aceptado, producción iniciada o dinero verificado; el servicio exige resolver también cualquier comprobante Pendiente antes de archivarlo.
- Una nueva cotización o diseño no autoriza por sí sola sublimación. Se comprueban versiones, aprobación y mínimo del 50 % con redondeo hacia arriba; cualquier aumento exige nueva evaluación.
- La vista vincula cada formulario al identificador del pedido que lo creó y descarta lecturas atrasadas al seleccionar otro. Al restaurar desde BFCache o volver visible, revalida sesión; una denegación limpia listado y expediente.

## Comprobación y límites de la entrega

Las pruebas HTTP/SQL se encuentran en `tests/ImpactoTextil.Api.Tests/AdministracionSqlTests.cs`. La suite completa de esta entrega ejecutó **83 pruebas, todas aprobadas y ninguna omitida**, incluidas 14 administrativas nuevas. Cada fixture crea y retira su propia base temporal; no elimina la base local ImpactoTextil del desarrollador. Los resultados locales están en `artifacts/administracion-20261031/completa.trx` y `administracion.trx`.

Comando reproducible desde PowerShell, para localhost con autenticación Windows:

```powershell
$env:IMPACTOTEXTIL_PRUEBAS_SQL = 'Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test ImpactoTextil.slnx -c Release --logger 'trx;LogFileName=completa.trx' --results-directory artifacts/administracion-20261031
```

Se incluyen permisos/CSRF, campos desconocidos, pertenencia, descarga exacta, archivos falsos, limpieza al rechazar, historial/decisiones, archivo administrativo, verificación/rechazo del mismo Pago, cobro idempotente y fechas distintas, dos administradores simultáneos, anticipo C$50.01 sobre C$100.01, aumento de total, diseño revisado, entrega y rollback tras un paso intermedio.

Se comprobaron además **13 recorridos de interfaz con Chrome headless, API y SQL reales en una base aislada**, sin errores JavaScript. Incluyen sesión real, cotización/aprobación, rechazo, subida/descarga exacta, persistencia al recargar, verificación del mismo Pago, aumento y bloqueo de sublimación, cobro con respuesta perdida y reintento, producción/entrega, archivo, lectura atrasada A/B y el control de restauración de sesión mediante evento pageshow persistido simulado. Se inspeccionó la pantalla a 1440 px y 320 px; no hubo desbordamiento horizontal a 320 px.

Evidencias conservadas: [resultados de navegador](../evidencias/administracion-pedidos-navegador.json), [83 resultados SQL/HTTP](../evidencias/administracion-suite-sql.json), [captura de escritorio](../evidencias/administracion-pedidos-escritorio.png) y [captura móvil](../evidencias/administracion-pedidos-movil-320.png). Las capturas utilizan identidades y archivos ficticios. Las pruebas automatizadas y la revisión de IA no sustituyen la revisión humana que realizará el líder.

La revisión del líder y del compañero todavía no se atribuye como realizada. Tampoco se acredita aquí la integración con los CU de Dylan, main, alojamiento público, carga de 50 usuarios, disponibilidad mensual ni medición SMART. El cierre de tu bloque local y el cierre del proyecto integrado son hitos distintos.
