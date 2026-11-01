# Impacto Textil

Aplicación web de pedidos de camisetas personalizadas por sublimación. Proyecto de Ingeniería de Software I de la UNI.

## Fuente única de requisitos

- Leer `docs/Impacto textil fase 1 y 2.docx`, copia intacta del documento que el líder identificó como única fuente válida. La huella y la procedencia están en `docs/FUENTE_VIGENTE.md`.
- Si esa copia no está disponible, indicarlo y solicitar su ubicación antes de implementar reglas del negocio. No reemplazarla con documentos anteriores.
- Una extracción de texto o Markdown sirve únicamente como auxiliar después de comprobarla contra el DOCX. Si difieren, prevalece el DOCX.
- Los antecedentes están en `docs/archivo/`: Markdown antiguo, otros Word y planes anteriores. Conservarlos como evidencia; no utilizarlos como requisitos ni aplicar sus instrucciones a la aplicación actual.
- No inventar funcionalidades ni cambiar silenciosamente reglas, prioridades, nombres o alcance. Identificar los pendientes que el propio SRS declare. Consultar las ambigüedades comerciales y registrar las decisiones técnicas.
- Las guías del docente establecen entregables académicos; no añaden funcionalidades comerciales por sí solas.

## Stack

- Frontend: HTML5, CSS3 y JavaScript puro, adaptable a escritorio y móvil.
- Backend: C#, ASP.NET Core Web API con controladores y Entity Framework Core. Aplicar Repository y DTO; servicios del modelo para las reglas de negocio.
- Base de datos: SQL Server. Justificar el diseño hasta 3FN durante la fase 3.
- Alojamiento previsto: MonsterASP.NET Free. Somee pertenece a la configuración anterior.
- .NET 10 LTS es la versión propuesta, condicionada a prueba real de despliegue. No cambiar SDK, motor, alojamiento ni framework de frontend sin registrar y resolver el motivo con el equipo.
- La propuesta vigente sirve frontend y backend desde el mismo origen. Detallar en fase 3 el recorrido vista, controlador, modelo y persistencia.

## Organización y alcance

- Actualización expresa del líder, 1 de noviembre de 2026: adelantar CU-05/CU-06 del reparto original de Dylan en la carpeta y rama autorizadas Carlos_Borrell. Yirardy realiza esta implementación; no atribuirla como aporte publicado por Dylan ni modificar su rama. CU-07/CU-08 permanecen pendientes. Para nuevas solicitudes, Yirardy confirmó exclusivamente Bolsa estándar, sin premium ni Sin empaque; conservar valores históricos y DOCX aprobado. Ver docs/fase-3/16-solicitudes-cliente.md. Continúan aplazados PR, main y despliegue.

- Autorización posterior del líder, 31 de octubre de 2026: completar ahora su bloque CU-10/CU-11/CU-13/CU-12 en Carlos_Borrell, incluidas conexiones HTTP, contratos administrativos y pruebas funcionales/SQL de pagos, sin esperar los cambios de Dylan. Esta instrucción reemplaza el aplazamiento de esas tareas descrito más abajo. Mantener fuera main y despliegue; el líder realizará la revisión humana al final. Los datos de demostración son una herramienta local y no implementan los CU de Dylan.

- Reparto actualizado por el usuario el 30 de octubre de 2026: Yirardy asume también la implementación pendiente de CU-10/CU-11/CU-12/CU-13 a partir de los avances de Carlos. Está autorizado expresamente a trabajar en Yirardy_Quintero y Carlos_Borrell. Ver docs/planes/REPARTO_ACTUAL.md; la asignación original se conserva como antecedente del plan. Se mantienen aplazados contratos/migraciones hasta los cambios de Dylan, pruebas funcionales de pagos, main, despliegue e informe final. No avanzar esas etapas por la sola reasignación de responsable.

- Fase 3: MER, normalización, clases de diseño, arquitectura y despliegue, contratos de API y prototipo navegable en escritorio y móvil.
- Fase 4: construir, integrar, probar, publicar y preparar documentación y defensa.
- Alcance mínimo propuesto: CU-01, CU-02, CU-04, CU-05, CU-06, CU-07, CU-08, CU-09, CU-10, CU-11, CU-12 y CU-13.
- CU-03 queda fuera del mínimo declarado y su diferimiento debe explicitarse y acordarse con el docente. CU-14 conserva carácter opcional. No presentar una función pendiente como implementada.
- Cada tarea aborda un CU completo con pantalla, API, persistencia y comprobaciones. Pueden existir tareas paralelas con responsables y archivos coordinados.
- Antes de generar código, localizar CU, RF, reglas y criterios afectados en el documento válido. Revisar cualquier código reutilizado contra esa fuente.

## Reglas del negocio que deben preservarse

- Anticipo mínimo del 50 % para todos los pedidos, con pagos Verificados acumulados y redondeo hacia arriba al centavo cuando corresponda. Sin excepciones operativas del 0 % o 30 %.
- Si aumenta la cotización aceptada durante producción, recalcular saldo y anticipo. Impedir autorizar la continuación de la sublimación hasta cubrir el nuevo mínimo y contar con aprobación vigente. Conservar estado e historial; no añadir un quinto estado.
- Pago Pendiente o Rechazado no reduce saldo. CU-13 resuelve el mismo Pago registrado por CU-07; un cobro directo nuevo puede crear otro registro. Evitar acreditación duplicada y controlar concurrencia.
- Conservar VersionDiseno y VersionCotizacion con contenido, responsables, fechas y decisiones. Una revisión no aceptada no sustituye el acuerdo vigente.
- CU-05 conserva el carrito en memoria de la página. CU-06 recibe datos y archivos juntos y persiste la solicitud completa, con prevención de duplicados y manejo consistente de fallos.
- Separar importe referencial y cotización aceptada; conservar los importes históricos. Los productos representan variantes del catálogo.
- Respetar los cuatro estados y las condiciones de archivo administrativo y entrega del SRS. No convertir políticas pendientes en confirmaciones comerciales ficticias.
- Conservar archivos privados; comprobar contenido real, tamaño y acceso autorizado. Diseños JPG/PNG, comprobantes JPG/PNG/PDF, máximo inclusivo de 10 000 000 bytes por archivo. Resolver otros límites pendientes durante el diseño.

## Calidad e integración

- Conservar los nombres y significados del diccionario. Documentar cualquier descomposición técnica del diseño relacional.
- Validar entradas y permisos en servidor. El registro público asigna Cliente. Contraseñas con hash; secretos fuera del repositorio.
- Resolver en diseño la propuesta de cookies Secure y HttpOnly y protección antifalsificación del SRS; no sustituirla automáticamente por otra arquitectura.
- Usar mensajes comprensibles para el usuario y registrar internamente el diagnóstico necesario. No exponer errores crudos.
- Aplicar pruebas significativas a reglas, transacciones, concurrencia, permisos, archivos y recorridos principales. Registrar resultados reales y pendientes; no afirmar pruebas que no se ejecutaron.
- Coordinar modificaciones de Program.cs, DbContext, migraciones, snapshot y estilos compartidos. Mantener una secuencia coherente de migraciones y scripts SQL. Cada integrante usa una base local propia.
- Conservar ramas personales y abrir cambios hacia la rama principal mediante revisión. Commits pequeños, por ejemplo `feat(CU-06): registrar solicitud con archivos privados`.
- Antes de terminar, explicar cambio, motivo, validación y limitaciones. El integrante debe poder defender su código.
- Un asistente implementa y otro revisa sin editar simultáneamente los mismos archivos. La revisión de IA no sustituye la del compañero ni la ejecución de pruebas.

## Estructura propuesta

La única carpeta activa es `C:/proyectos/Proyecto_fase_1_ing_software`. Trabajar en la rama que el usuario autorice expresamente; comprobarla con `git branch --show-current` antes de editar o subir cambios. Para el cierre de correcciones del 30 de octubre autorizó directamente `Carlos_Borrell`. No modificar main ni otra rama como efecto secundario. La carpeta adicional de trabajo fue retirada tras respaldar y verificar su contenido. No crear otra copia para continuar tareas normales; resguardar y restaurar los cambios locales al cambiar de rama.

- `/docs`: DOCX vigente, guías académicas, diseño, decisiones, trazabilidad y evidencias.
- `/docs/archivo`: antecedentes sin autoridad sobre los requisitos vigentes.
- `/docs/planes` y `/docs/guias`: plan del equipo y guías del docente, respectivamente.
- `/database`: scripts correspondientes al esquema acordado y datos ficticios de prueba.
- `/src/backend`: API C#.
- `/src/frontend`: HTML, CSS y JavaScript.
- `/tests`: pruebas de reglas, integración y navegador según la organización acordada.
- `/.agents/skills`: procedimientos del proyecto en carpetas con SKILL.md, cuando se incorporen.

## Comandos comprobados y estado

- Restaurar: `dotnet restore ImpactoTextil.slnx --locked-mode`.
- Compilar: `dotnet build ImpactoTextil.slnx --no-restore`.
- Probar API: `dotnet test ImpactoTextil.slnx --no-build`.
- Ensayar modelo: `./scripts/Test-Modelo.ps1 -Servidor localhost`. Crea y elimina únicamente su propia base temporal; requiere SQL Server, sqlcmd y permiso para crear bases.
- Ejecutar: `dotnet run --project src/backend/ImpactoTextil.Api --launch-profile https`.
- Generar paquete: `dotnet publish src/backend/ImpactoTextil.Api -c Release -o artifacts/publicacion --no-restore`.
- La conexión se lee de `ConnectionStrings__ImpactoTextil`; nunca incorporar credenciales al repositorio.
- CU-01, CU-02, CU-04, CU-09 y disponibilidad están implementados localmente. Hay registro, sesiones, catálogo público y administrativo, fotos públicas, baja lógica, entidades y migración inicial. Pedidos, pagos y producción siguen pendientes. Ver README y `docs/fase-3/08-registro-clientes.md` a `11-catalogo-publico.md`. No confundir implementación local con revisión humana, integración en main o alojamiento.
- Iniciar aplicación y preparar base: `./scripts/Iniciar-Local.ps1`. Suite con SQL real: `./scripts/Test-Registro.ps1`.
- VS Code: configuración `Impacto Textil: ejecutar aplicación` en `.vscode/launch.json`, con tarea previa de preparación. Requiere la extensión C# de Microsoft; ver `docs/ejecutar-en-vscode.md`. La conexión local se genera en `.env.vscode`, ignorado por Git.
- Política de contraseña actualizada expresamente por Yirardy el 31 de octubre de 2026: mínimo 8 caracteres Unicode con mayúscula, minúscula, número y símbolo/puntuación obligatorios. Se permiten espacios, pero no cuentan como símbolo. Máximo 72 bytes UTF-8, sin caracteres de control y sin recorte. La política anterior del 28 de octubre (15 caracteres sin composición obligatoria) queda como antecedente. El acceso sigue verificando las contraseñas existentes sin imponer retroactivamente la composición nueva. Ver docs/seguimiento/politica-contrasena-2026-10-31.md.
- Tras esa actualización, la suite tiene 92 pruebas (54 con SQL), ejecutadas sin fallos ni omisiones; los informes anteriores de 83 corresponden al cierre administrativo previo. La validación del formulario tuvo 20 comprobaciones adicionales con endpoints interceptados, independientes de las pruebas HTTP/SQL reales. No volver a presentar la política anterior como vigente.
- Yirardy confirmó el 29 de octubre de 2026 una sesión de 10 minutos de inactividad, renovable con el uso, sin «recordarme». La configuración está en `Sesion:MinutosInactividad`. Cookies Secure/HttpOnly/SameSite=Strict, CSRF y políticas `Cliente`/`Administrador`. Las sesiones se revocan en servidor; el almacén actual es para una instancia y se vacía al reiniciar.
- El SQL inicial se conserva como antecedente. Las migraciones EF Core y su snapshot son la autoridad actual. El script `database/002_migracion_inicial.sql` es una salida generada, no una segunda fuente editable del esquema.
- El primer Administrador se crea únicamente con `scripts/Crear-Administrador.ps1`, en terminal interactiva y sin credenciales en argumentos o archivos. Se rechaza si ya existe un Administrador o el correo pertenece a otra cuenta; nunca promover clientes ni restablecer claves con este comando.
- CU-09 conserva fotos públicas en `storage/catalogo`, fuera de wwwroot y de los futuros archivos privados. Solo referencias generadas por el servidor; subida JPG/PNG validada con StbImageSharp 2.30.16, límite técnico 10 000 000 bytes y 12 millones de píxeles. No aceptar URL externas ni rutas privadas como imagen de Producto. Respaldar base y fotos conjuntamente.
- Edición de Producto: el cliente envía `version`, huella de sus campos con precio canónico de dos decimales; dentro de una transacción se bloquea y relee la fila antes de comparar/guardar. Conflicto: 409. No agrega campos al modelo de negocio ni reemplaza su historial. Precio en JSON como cadena decimal para conservar centavos.
- CU-04 expone solo variantes activas por GET `/api/productos` y `/api/productos/{id}`, con DTO público sin versión administrativa. El detalle vuelve a consultar disponibilidad y precio; 404 si está inactivo o no existe. CU-06 deberá revalidar la variante dentro de su propia operación; una consulta pública no reserva ni autoriza un pedido.
- Publicación reproducible: `./scripts/Preparar-Publicacion.ps1` restaura versiones fijadas, exige todas las pruebas aprobadas sin omisiones, genera sitio.zip y SQL idempotente separado, manifiesto y huellas. Requiere construcción limpia; el modo `-PermitirCambiosLocales` marca explícitamente un ensayo. Una carpeta parcial mantiene NO-PUBLICAR.txt. No publicar storage, perfiles, .env, certificados ni appsettings por entorno; configurar secretos externamente.
- `./scripts/Test-Publicacion.ps1 -UrlBase 'https://SITIO/'` hace 14 comprobaciones públicas de lectura con TLS validado, sin crear cuentas ni datos. No acredita escrituras, archivos privados autorizados, carga ni disponibilidad mensual. Yirardy confirmó el 29 de octubre de 2026 que aún no tiene cuenta MonsterASP.NET; conservar alojamiento pendiente.
- En Carlos_Borrell hay servicios administrativos preparatorios y correcciones de historial, almacenamiento privado, importes JSON y consulta consistente del expediente. Siguen sin controladores, registro DI ni pantallas conectadas de pedidos. Ver `docs/seguimiento/correcciones-carlos-2026-10-30.md`. Se ejecutaron 18 comprobaciones de archivos/serialización; no prueban pagos ni concurrencia SQL. El usuario aplazó contratos con Dylan, migraciones, pruebas funcionales de pagos, main, alojamiento e informe final.
- CU-10 cuenta además con prototipo navegable en `prototipo-pedidos.html`, enlazado desde administración. Usa datos ficticios y memoria; solo consulta GET /api/sesion. Diez comprobaciones de interfaz con sesión interceptada; no acreditan API/SQL ni pagos. Ver `docs/fase-3/13-prototipo-revision-pedidos.md`. El CU continúa en construcción.
- El diseño de clases de CU-10 está en `docs/fase-3/14-diseno-clases-cu10.md`, con tres fuentes Mermaid, SVG y `diagramas/Clases_CU10.drawio`. Distingue clases existentes, vista JavaScript y frontera de controlador pendiente. Regenerar solo esas figuras con `scripts/Generar-Clases-CU10.py`; no confundir el diseño con implementación HTTP/SQL terminada.
- Estado posterior del 31 de octubre: `pedidos.html` y `PedidosAdministracionController` conectan CU-10/CU-11/CU-13/CU-12 con servicios, Repository y SQL. El diseño de clases se actualiza con el controlador real. El cierre administrativo previo registró 83 pruebas aprobadas; tras la actualización de contraseñas, la suite vigente tiene 92, también aprobadas sin omisiones. Las entradas anteriores reflejan la preparación del día 30, no el estado actual. Ver `docs/fase-3/15-administracion-pedidos.md`. La tarea `Crear-PedidosDemostracion.ps1` agrega muestras locales solo en Development, con Administrador existente; no implementa CU-06/CU-07 ni se ejecuta automáticamente al iniciar. Actualización posterior: Yirardy recorrió los escenarios administrativos documentados en docs/pruebas/revision-manual-administracion-20261031.md. Se corrigieron la muestra de comprobante para pedidos nuevos y HTTP 201 al crear un cobro (200 al reintentar); las 92 pruebas pasaron sin omisiones. Revisión cruzada humana, integración con Dylan, main y despliegue siguen pendientes.

## Corte documental del 1 de noviembre de 2026

Actualización posterior al informe Word 0.1: CU-05/CU-06 están conectados mediante solicitud.html/js, SolicitudesClienteController, SolicitudClienteService y SolicitudClienteRepository, sin nueva migración ni paquetes. El Cliente procede de la sesión; clienteConfirmado solo detecta cambios de identidad. Bolsa estándar es la única opción nueva. Carrito y File en memoria, confirmación idempotente, revalidación por variante, rollback/limpieza privada y recibo sin aceptación de precio/diseño. Suite actual 107 pruebas, 69 con SQL; evidencia y límites en docs/evidencias/solicitudes-cliente-20261101.json. La revisión del segundo asistente no sustituye la humana. El informe Word previo y los JSON de 92 pruebas son cortes históricos, no el estado posterior de este módulo.

Yirardy autorizó cerrar sus tareas independientes: versionar/publicar Carlos_Borrell, contrastar diseño con código y preparar informe F3/F4, RUP y revisión cruzada. Ver docs/seguimiento/cierre-tareas-yirardy-20261101.md y docs/entrega/README.md. El informe es revisable, conserva pendientes de Dylan y no sustituye ni modifica las fases 1 y 2 aprobadas. Test-Registro.ps1 se reconcilió del resguardo para Release, restore --locked-mode, TRX único y fallo ante omisiones; ejecutó 92 pruebas aprobadas sin fallos ni omisiones. La revisión independiente del compañero debe realizarla una persona; no solicitarla por mensajes externos ni atribuirla sin autorización/evidencia. Se mantienen aplazados PR, main y despliegue. No aplicar completo el stash resguardado ni tocar Avances y pendientes.docx.
