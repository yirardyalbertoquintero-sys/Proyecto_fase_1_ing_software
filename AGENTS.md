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

La única carpeta activa es `C:/proyectos/Proyecto_fase_1_ing_software`, en la rama personal `Yirardy_Quintero`. La carpeta adicional de trabajo fue retirada tras respaldar y verificar su contenido. No crear otra copia para continuar tareas normales.

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
- CU-01, CU-02, CU-09 y disponibilidad están implementados localmente. Hay registro, sesiones, administración de variantes, fotos públicas, baja lógica, entidades y migración inicial. Catálogo público CU-04 y los demás CU siguen pendientes. Ver README y `docs/fase-3/08-registro-clientes.md`, `09-acceso-sesiones.md`, `10-catalogo-administracion.md`.
- Iniciar aplicación y preparar base: `./scripts/Iniciar-Local.ps1`. Suite con SQL real: `./scripts/Test-Registro.ps1`.
- VS Code: configuración `Impacto Textil: ejecutar aplicación` en `.vscode/launch.json`, con tarea previa de preparación. Requiere la extensión C# de Microsoft; ver `docs/ejecutar-en-vscode.md`. La conexión local se genera en `.env.vscode`, ignorado por Git.
- La política de contraseña confirmada por Yirardy el 28 de octubre de 2026 exige al menos 15 caracteres Unicode, admite espacios y no exige símbolos. Rechazar más de 72 bytes UTF-8 antes de BCrypt; no modificar ni recortar la contraseña.
- Yirardy confirmó el 29 de octubre de 2026 una sesión de 10 minutos de inactividad, renovable con el uso, sin «recordarme». La configuración está en `Sesion:MinutosInactividad`. Cookies Secure/HttpOnly/SameSite=Strict, CSRF y políticas `Cliente`/`Administrador`. Las sesiones se revocan en servidor; el almacén actual es para una instancia y se vacía al reiniciar.
- El SQL inicial se conserva como antecedente. Las migraciones EF Core y su snapshot son la autoridad actual. El script `database/002_migracion_inicial.sql` es una salida generada, no una segunda fuente editable del esquema.
- El primer Administrador se crea únicamente con `scripts/Crear-Administrador.ps1`, en terminal interactiva y sin credenciales en argumentos o archivos. Se rechaza si ya existe un Administrador o el correo pertenece a otra cuenta; nunca promover clientes ni restablecer claves con este comando.
- CU-09 conserva fotos públicas en `storage/catalogo`, fuera de wwwroot y de los futuros archivos privados. Solo referencias generadas por el servidor; subida JPG/PNG validada con StbImageSharp 2.30.16, límite técnico 10 000 000 bytes y 12 millones de píxeles. No aceptar URL externas ni rutas privadas como imagen de Producto. Respaldar base y fotos conjuntamente.
- Edición de Producto: el cliente envía `version`, huella de sus campos con precio canónico de dos decimales; dentro de una transacción se bloquea y relee la fila antes de comparar/guardar. Conflicto: 409. No agrega campos al modelo de negocio ni reemplaza su historial. Precio en JSON como cadena decimal para conservar centavos.
