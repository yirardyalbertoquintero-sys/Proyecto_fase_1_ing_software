# Plan de trabajo para las fases 3 y 4 de Impacto Textil

Propuesta de organización para Yirardy, Dylan y Carlos, elaborada el 27 de octubre de 2026 y actualizada con el documento que el líder identificó como única fuente válida. Los tres tienen un nivel similar y ya disponen de ramas personales en GitHub. Se propone que Yirardy coordine como líder. El reparto puede intercambiarse por acuerdo del grupo; lo importante es conservar responsables claros y revisiones cruzadas.

La fase 3 transforma el análisis en diseño técnico y prototipos. La fase 4 construye, integra, prueba y prepara la entrega. Cada integrante construirá módulos completos con pantalla, API, datos y pruebas. El trabajo avanzará por entregas pequeñas que se incorporen continuamente a una aplicación común.

## 1 Fuentes y alcance

La única fuente de requisitos del negocio es Impacto textil fase 1 y 2.docx, identificado expresamente por el líder y localizado en C:/proyectos/Impacto textil fase 1 y 2.docx. Su huella SHA-256 al revisar esta versión es 9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821. Las exigencias académicas se contrastan con PLAN DE EJECUCIÓN PROYECTO DE CURSO - INGENIERÍA DE SOFTWARE I.pdf y FASES DEL `PROYECTO EXPLIACION AMPLIADA .pdf. El texto de Antigravity y los otros archivos del repositorio se consideran antecedentes; no aportan requisitos aprobados.

El documento vigente debe copiarse intacto a docs/Impacto textil fase 1 y 2.docx y registrarse en el repositorio. Si se crea una extracción Markdown para las herramientas de asistencia, debe proceder de ese Word, verificarse contra él y etiquetarse como copia auxiliar; el Word seguirá siendo la autoridad. La antigua ruta docs/fase-1-y-2-ing-de-software.md deja de ser fuente válida. No utilizar documentos anteriores para rellenar ambigüedades. Las actualizaciones expresamente aprobadas por el equipo deberán registrarse con fecha y trazabilidad, sin modificar silenciosamente el documento aceptado.

La meta de construcción son los doce casos previstos en la sección 6.1: CU-01, CU-02, CU-04, CU-05, CU-06, CU-07, CU-08, CU-09, CU-10, CU-11, CU-12 y CU-13. Incluyen los seis principales: CU-02, CU-05, CU-06, CU-10, CU-12 y CU-13.

El plan de ejecución docente, página 4, exige al menos el 80 % de los casos de uso principales conectados a una base real. Con seis principales, cinco representan el 83.33 %; la meta del equipo seguirá siendo completar los seis, más las funciones de apoyo del MVP. Esa exigencia no autoriza descartar requisitos de prioridad Alta.

CU-03, recuperación automatizada, queda fuera del mínimo declarado; el diferimiento debe explicitarse y acordarse con el docente. CU-14, reporte mensual, conserva su carácter opcional. Una recuperación manual no demuestra RF-13. No añadir inventario, pasarela bancaria, facturación fiscal u otros módulos ajenos al SRS.

## 2 Tecnología y estructura común

- Frontend: HTML, CSS y JavaScript puro, con escritorio y móvil.
- Backend: C#, ASP.NET Core Web API, Entity Framework Core, Repository y DTO; reglas de negocio en servicios del modelo.
- Base de datos: SQL Server, con diseño y justificación de normalización hasta 3FN.
- Alojamiento inicial: MonsterASP.NET Free. Somee corresponde al plan antiguo.
- Versión propuesta en el documento: .NET 10 LTS, condicionada a la prueba de alojamiento. No cambiar versión ni proveedor silenciosamente si la prueba falla.

Estructura acordada: /docs para documentación; /database para scripts y datos de prueba; /src/backend para la API; /src/frontend para las pantallas; /.agents/skills para instrucciones del proyecto, si se utilizan. Definir una publicación reproducible que sirva el frontend y backend del mismo origen, conforme a la propuesta vigente.

En el diseño debe poder seguirse este recorrido: navegador y vista → controlador HTTP → servicios del modelo → repositorio y EF Core → SQL Server. Los DTO definen los datos que entran y salen de la API. El despliegue representará los componentes reales de ASP.NET Core y del alojamiento seleccionado; los ejemplos de otros servidores en la guía no obligan a incorporarlos al stack.

## 3 Reparto de módulos

| Integrante | Casos de uso a su cargo | Resultado funcional |
| --- | --- | --- |
| Yirardy | CU-01 Registrar nueva cuenta; CU-02 Iniciar sesión; CU-04 Consultar catálogo de productos; CU-09 Gestionar catálogo | Acceso, roles y catálogo público y administrativo. Coordina la estructura común, la integración de migraciones y la publicación. |
| Dylan | CU-05 Configurar prenda y diseño; CU-06 Registrar solicitud de pedido; CU-07 Registrar comprobante de pago; CU-08 Consultar estado y saldo | Recorrido del cliente: configuración, solicitud, comprobantes y seguimiento de sus pedidos. |
| Carlos | CU-10 Revisar y cotizar pedidos; CU-11 Descargar archivo de diseño; CU-13 Verificar y registrar pagos; CU-12 Actualizar producción y entrega | Recorrido administrativo: cotizaciones y diseños con historial, pagos, controles de producción y entrega. El archivo administrativo de solicitudes se integra en la revisión del pedido. |

Cuatro casos por persona no equivalen al mismo esfuerzo. CU-10, CU-12 y CU-13 concentran reglas importantes. Cuando Yirardy termine acceso y catálogo, apoyará a Carlos en pruebas de pagos y concurrencia e integración de pantallas administrativas. Dylan preparará los escenarios de cliente que atraviesan cotización, pago y producción. Revisar el reparto según tareas terminadas y bloqueos reales.

Cada responsable entrega los cambios de su módulo en pantalla, API, modelo y persistencia, las pruebas pertinentes y su trazabilidad. Coordinar una parte común no significa programar todo el trabajo de los compañeros.

La revisión se rota: Dylan revisa a Yirardy; Carlos revisa a Dylan; Yirardy revisa a Carlos. Si un revisor colaboró directamente en una modificación, el tercer integrante revisa esa parte.

## 4 Trabajo de la fase 3

Antes de construir módulos en paralelo, acordar las decisiones compartidas. Pueden trabajar simultáneamente sobre sus partes del diseño y revisarlas juntos.

| Entregable | Coordinación propuesta | Participación y comprobación |
| --- | --- | --- |
| MER lógico y relacional con PK, FK, cardinalidades y justificación hasta 3FN | Yirardy | Todos aportan entidades y relaciones de sus módulos; revisar pagos, detalles y versiones entre los tres. |
| Clases de diseño con tipos de C#, propiedades y patrones | Carlos | Cada integrante detalla sus clases, servicios, repositorios y DTO; contrastar con el diccionario. |
| Arquitectura y despliegue | Yirardy | Verificar navegación, API, SQL Server, almacenamiento privado y publicación real. |
| Prototipo navegable de alta fidelidad en escritorio y móvil | Dylan | Cada responsable diseña sus pantallas y estados de error; unirlos en un recorrido de cliente y administrador. |
| Contratos de API y matriz de construcción | Los tres | Acordar entradas, salidas, permisos, validaciones, errores y dependencia de cada CU. |

El MER debe contemplar Usuario, Producto, Pedido, DetallePedido, Pago, Constancia y los historiales VersionDiseno y VersionCotizacion definidos en el análisis. Si una cotización tiene varios importes por detalle, resolver su representación relacional durante el diseño. La recuperación solo requiere su modelo adicional si se implementa CU-03.

Conservar nombres y significados del diccionario. Toda descomposición técnica necesaria para SQL debe quedar vinculada al concepto lógico correspondiente. Identificar claves candidatas y dependencias: añadir un identificador artificial no constituye por sí solo una justificación de 3FN. Distinguir valores calculados de valores históricos que deben conservarse.

Para cada API, acordar método y ruta, datos obligatorios, DTO de respuesta, rol permitido, titularidad del pedido, errores comprensibles y resultado de la operación. Las rutas son decisiones de diseño; no deben añadir funcionalidades nuevas. Usar una sola solución de autenticación y un formato común de errores.

La fase 3 estará lista cuando los tres puedan explicar el recorrido de un pedido en el prototipo, localizar sus datos en el MER y señalar qué parte del backend aplica cada regla. Registrar en /docs las decisiones y los pendientes reales. La aceptación comercial de una cotización se registra como establece el SRS; no inventar un nuevo canal de aprobación.

## 5 Orden de construcción de la fase 4

### Bloque inicial de integración

Yirardy prepara la base común y CU-01/CU-02; después CU-09/CU-04. En paralelo, Dylan avanza el frontend de CU-05 y Carlos prepara servicios, contratos y pruebas de las reglas administrativas acordadas. El código que use datos simulados solo sirve para desarrollar interfaces: no acredita un caso terminado.

Realizar pronto una publicación mínima en MonsterASP.NET Free. Comprobar ejecución de la versión elegida, conexión a SQL Server, HTTPS, persistencia y descarga autorizada de archivos privados. No esperar al último día para descubrir una incompatibilidad.

### Solicitudes y primera consulta

Dylan conecta CU-05 y CU-06 con autenticación, variantes reales del catálogo y archivos. Después conecta CU-08 para consultar pedidos propios, aunque aún estén Pendientes. La consulta no necesita esperar a que todo el módulo administrativo esté terminado. Si no existe cotización aceptada, indicarlo expresamente; no presentar como definitivo un precio referencial.

Carlos conecta CU-10 y CU-11 en cuanto existan solicitudes reales. Deben funcionar la cotización aceptada, el registro de aprobación y el historial de diseños y cotizaciones.

### Comprobantes y pagos

Con una cotización aceptada, Dylan integra CU-07 con Carlos en CU-13. Probar el circuito completo: evidencia Pendiente → verificación o rechazo del mismo Pago → saldo actualizado únicamente si corresponde. Completar también cobro directo con constancia.

### Producción y entrega

Carlos integra CU-12 con diseños, cotizaciones y pagos; Dylan refleja los resultados en CU-08. Probar tanto el inicio como la revalidación del anticipo cuando aumenta el total aceptado durante producción. Incorporar el archivo administrativo bajo las condiciones del SRS.

### Cierre funcional

Los tres ejecutan el recorrido completo con SQL Server y usuarios de prueba. Corregir fallos antes de abordar opcionales. Las pruebas, la documentación y la publicación se actualizan durante todos los bloques; este cierre confirma su funcionamiento conjunto.

Cada integrante mantendrá un CU activo a la vez. Esto permite hasta tres casos en paralelo cuando sus dependencias estén disponibles. Los bloques expresan precedencias y no fechas garantizadas; el grupo ajustará su calendario cuando el docente confirme la defensa.

## 6 Reglas que deben respetarse al programar

1. El anticipo mínimo es 50 % para todos los pedidos. No implementar excepciones operativas del 0 % o 30 %. Acumular únicamente pagos Verificados y redondear hacia arriba al centavo cuando corresponda.
2. Si un pedido de C$1,000 tiene C$500 pagados y se acepta un total de C$1,400, exigir C$700 de anticipo: faltan C$200 antes de continuar sublimando. Conservar En producción y los pagos existentes. La autorización de sublimación es un control de CU-12, no un quinto estado.
3. Pendiente y Rechazado no reducen el saldo. Resolver la evidencia existente sin duplicar el cobro. Conservar la decisión administrativa de rechazo; diferenciarla de revertir una operación técnica fallida. Impedir la acreditación repetida y controlar decisiones concurrentes.
4. Conservar versiones de diseño y cotización, archivo o importes, fecha, responsable y aprobación o decisión. Una propuesta rechazada no sustituye el acuerdo vigente. Un cambio obliga a comprobar las versiones vigentes antes de sublimar.
5. CU-05 conserva el carrito en memoria de la página. CU-06 envía datos y archivos juntos y persiste la solicitud completa. Validar en servidor el contenido real, los permisos y los límites; evitar solicitudes duplicadas por reintento. La recarga del navegador no tiene persistencia prometida.
6. Diseños JPG/PNG; comprobantes JPG/PNG/PDF; máximo inclusivo de 10,000,000 bytes. Archivos privados con descarga autorizada y comprobación de titularidad o rol. Una prenda Sin personalización no exige imagen.
7. Producto representa una variante concreta del catálogo con talla, color y tela. No añadir inventario ni opciones libres ajenas al modelo. Separar precio referencial de cotización aceptada y conservar los importes históricos.
8. Mantener cuatro estados de producción: Pendiente, En producción, Listo para entrega y Entregado. El archivo administrativo es independiente y solo procede bajo las condiciones vigentes; conservar solicitud e historial.
9. La entrega con saldo cero está planteada como política de la demostración académica pendiente de validación comercial. Conservar esa distinción y registrar la decisión de la propietaria cuando exista. La confirmación verbal del anticipo no debe convertirse en una constancia documental ficticia.
10. Registro público solo como Cliente; contraseñas con hash; comprobación de permisos en servidor; mensajes de error comprensibles. Mantener secretos fuera del repositorio. Los pagos se realizan fuera del sistema; no almacenar datos bancarios de tarjetas ni simular una pasarela.

## 7 Trabajo con las ramas que ya existen

Usar la rama principal que ya tenga el repositorio, sea main u otro nombre, como versión integrada. Conservar las tres ramas personales; no hace falta sustituirlas para aplicar este plan.

1. Elegir una tarea concreta identificada por CU y registrar quién la realiza y revisa.
2. Actualizar la rama personal con la principal antes de comenzar una tarea y después de integrar cambios de compañeros.
3. Implementar y comprobar la tarea con la base local. Hacer commits pequeños, por ejemplo: feat(CU-06): registrar solicitud con archivos privados.
4. Abrir un pull request desde la rama personal hacia la principal. Explicar el cambio y cómo se comprobó.
5. El compañero revisa. Resolver conflictos comprendiendo ambas modificaciones y volver a comprobar el resultado; no escoger automáticamente todo el contenido de una rama.
6. Integrar cuando compile y funcione el recorrido afectado. Los demás actualizan sus ramas antes de continuar.

No acumular todos los módulos en tres ramas aisladas hasta el final. Acordar antes de editar archivos compartidos como Program.cs, DbContext, el snapshot de EF, estilos comunes y documentos centrales. Cada persona puede desarrollar sus entidades y configuraciones; Yirardy coordina cuándo se generan e integran migraciones para mantener una secuencia coherente. Los scripts de /database deben corresponder a esa misma secuencia, no constituir un esquema distinto mantenido por separado.

Cada integrante utiliza su propia base local. Reservar el entorno común para pruebas de integración controladas. Antes de cambiar un contrato o un esquema que otro módulo consume, avisar al grupo y actualizar la documentación.

Referencias técnicas para este acuerdo: Microsoft, Migrations in Team Environments, https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/teams; GitHub, Merge conflicts, https://docs.github.com/en/pull-requests/reference/merge-conflicts. La distribución de coordinadores es una propuesta para este equipo.

## 8 Evidencias y condición de terminado

Un CU termina cuando funciona desde su pantalla hasta SQL Server; cumple permisos, validaciones y alternativas del SRS; supera las comprobaciones relevantes; tiene revisión de otro integrante; está integrado y su trazabilidad se actualizó. Una pantalla aislada o un endpoint sin conectar no bastan.

Conservar evidencia de los escenarios críticos: sesión inválida; acceso a pedidos o archivos ajenos; archivo inválido y tamaño límite; solicitud repetida o guardado fallido; aceptación y rechazo de versiones; pago pendiente, rechazado y verificado; doble verificación y concurrencia; anticipo insuficiente; aumento de cotización en producción; diseño vigente sin aprobar; transición o entrega inválida; archivo administrativo permitido y denegado.

Registrar CU/RF/RNF, escenario, datos, resultado esperado y observado, responsable, fecha y evidencia o commit. Un ensayo pendiente o fallido debe figurar como tal. La declaración de funcionamiento se basa en ejecuciones reales.

Aplicar los RNF con sus alcances documentados. RNF-02 mide la carga completa de imagen, incluida la red, con media menor de tres segundos; no solo el procesamiento del servidor. RNF-05 exige comprobar 50 usuarios concurrentes e integridad, con el protocolo y los umbrales propuestos del SRS. La disponibilidad mensual del 99 % requiere observar un mes completo; una defensa breve no la demuestra. Las reducciones SMART del 60 % y 80 % se evalúan mediante la comparación programada, no por mostrar pantallas.

## 9 Informe y defensa

Consolidar las fases 1 a 4 en un solo informe, incorporando el diseño, prototipo, construcción y resultados. Preparar la tabla RUP con Inicio, Elaboración, Construcción y Transición y su relación con los artefactos y las disciplinas; las cuatro fases del curso no son una equivalencia automática uno a uno con RUP.

Aplicar las normas de la página 5 del plan docente: portada institucional con integrantes y carnés; historial de versiones después de la portada; Arial o Times New Roman de 12 puntos, interlineado 1.5 y títulos de 14 en negrita; numeración inferior derecha excepto portada e índice; pies de figura centrados y diagramas vectoriales o de alta resolución; encuadernación y declaración de originalidad firmada por los integrantes. Declarar con veracidad el uso de asistencia de IA y no fabricar firmas.

Incluir URL del repositorio, instrucciones reproducibles, scripts y usuarios de demostración. El recuadro de credenciales será exclusivamente de prueba y no contendrá secretos de infraestructura. Preparar también la entrega digital indicada por la guía, en PDF y ZIP por Aula Virtual, según la instrucción final del docente.

Hay dos ajustes documentales que deben quedar incorporados en la consolidación:

- La sección 6.1 y la referencia a porcentajes de 5.5 todavía indican que el 80 % no está acreditado. El nuevo plan de ejecución, página 4, sí lo exige para los CU principales. Actualizar esa referencia en la consolidación académica, conservando la meta de seis principales completos y sin cambiar el alcance comercial aceptado.
- Las guías muestran calendarios distintos: la explicación ampliada señala aproximadamente 24–28 de octubre y el plan de ejecución indica 4–7 de noviembre. También difiere el número de semanas. Mantener la fecha efectiva de entrega o defensa pendiente de confirmación, conforme a lo comunicado por el equipo; no presentar una de las fechas como confirmación recibida.

Ensayar con cliente y administrador: registrar solicitud → cotizar y aprobar diseño → cargar evidencia → verificar pago → iniciar producción → aceptar aumento y bloquear sublimación hasta completar anticipo → completar producción → entregar. Explicar además un rechazo de comprobante y una operación denegada por permisos. Los tres deben poder explicar las decisiones y localizar el código, aunque cada uno presente primero sus módulos.

## 10 Primeras acciones del equipo

1. Registrar en /docs la fuente vigente y este plan, dejando el plan antiguo como antecedente identificado.
2. Aceptar o intercambiar los responsables propuestos y crear tareas por CU con su revisor y dependencias.
3. Completar MER, clases, arquitectura, prototipo navegable y contratos. Registrar las políticas o límites que aún estén pendientes de validación; no inventarlos durante la generación de código.
4. Integrar la base común y probar alojamiento, SQL Server y archivos privados.
5. Construir e integrar los doce CU en el orden descrito, manteniendo revisiones, pruebas y documentación durante el desarrollo.
6. Corregir las referencias académicas nuevas en el informe consolidado y preparar evidencias y defensa con la fecha finalmente confirmada.

Este plan organiza trabajo pendiente: no certifica que el código, el despliegue, las pruebas o las métricas ya estén terminados.

## 11 Arranque paso a paso para el líder y el equipo

### Paso 1 Establecer la base compartida

Responsable: Yirardy. Revisor: Dylan.

1. Comprobar qué cambios locales y ramas existen antes de integrar o actualizar; conservar el trabajo existente.
2. Copiar el DOCX aprobado a la ruta de docs indicada arriba y comprobar que la copia es idéntica al original.
3. Actualizar AGENTS.md: fuente única correcta, MonsterASP.NET Free y reglas vigentes. El archivo preparado junto a este plan es una propuesta para incorporar; todavía no acredita un cambio en el repositorio.
4. Conservar los otros archivos como antecedentes identificados, sin utilizarlos para decidir requisitos y sin borrarlos.
5. Abrir una tarea por entregable de diseño y por CU. Cada tarea tendrá responsable, revisor, dependencias, criterio de aceptación y evidencia requerida.
6. Integrar estos cambios de preparación mediante revisión en la rama principal. Los tres sincronizan después sus ramas personales.

Resultado para avanzar: los tres pueden abrir el mismo DOCX, indicar su versión y describir el mismo alcance y stack. No se necesita que el código anterior sea correcto para cerrar esta preparación.

### Paso 2 Diseñar en paralelo

| Integrante | Trabajo de esta primera ronda | Qué debe entregar al grupo |
| --- | --- | --- |
| Yirardy | Borrador global del MER y arquitectura. Coordinar cómo se representan usuarios, variantes, pedidos, detalles, pagos, constancias e historiales. | MER editable; relaciones, claves y justificación de 3FN; arquitectura y borrador de despliegue. |
| Dylan | Inventario de pantallas y prototipo navegable. Comenzar por catálogo, configuración, solicitud y seguimiento; incorporar con Carlos las pantallas administrativas. | Navegación cliente y administrador, escritorio y móvil, con estados vacíos, validaciones y errores. |
| Carlos | Clases de diseño y contratos de API. Precisar servicios y operaciones para cotización, versiones, pagos y producción; acordar con los otros dos las operaciones de sus módulos. | Clases con tipos de C#; catálogo de operaciones, DTO, roles, errores y condiciones de negocio. |

Cada integrante consulta el mismo SRS y comparte avances durante la ronda. No se construyen tres modelos de datos, tres diseños de autenticación o tres estructuras de proyecto independientes. Los coordinadores incorporan aportes de todos y los tres comprenden el conjunto.

### Paso 3 Revisar las conexiones del diseño

Responsables: los tres; coordina Yirardy.

Recorrer solicitud, cotización, aprobación, pago, producción y entrega. Por cada acción, identificar pantalla, operación de API, servicio, datos y criterio del SRS. Confirmar que la representación de las versiones y los pagos coincide en los tres diseños. Registrar las decisiones técnicas y los pendientes comerciales que el propio documento conserva.

Resultado para avanzar: MER, clases, contratos y prototipo son compatibles. Se conocen el mecanismo de autenticación que se usará, los permisos, el cálculo monetario, el almacenamiento privado y la secuencia de migraciones. La propuesta de cookies y protección antifalsificación del SRS debe resolverse en diseño; no sustituirla automáticamente por otra solución.

### Paso 4 Preparar y comprobar la base ejecutable

Responsable de integración: Yirardy. Dylan y Carlos reproducen la instalación y revisan.

Preparar una sola solución, estructura, configuración de versiones, exclusión de secretos, estilo común, formato de errores y proyectos de pruebas. Probar SQL Server local y la publicación mínima en MonsterASP.NET Free con HTTPS y archivos privados. Documentar los pasos reales en README. Las pruebas técnicas pueden comenzar durante el diseño para comprobar viabilidad, pero no sustituyen los entregables de fase 3.

Resultado para avanzar: los tres pueden ejecutar la base y utilizar su propia base local; existe una decisión comprobada sobre versión y alojamiento. Si falla la publicación, documentar y resolver el impedimento con el grupo antes de dar por confirmada la tecnología de despliegue.

### Paso 5 Construir por entregas verificables

| Entrega | Yirardy | Dylan | Carlos | Condición de cierre |
| --- | --- | --- | --- | --- |
| Acceso y catálogo | Completar CU-01 y CU-02; después CU-09 y CU-04. | Preparar interfaz y lógica temporal de CU-05 según contratos. | Preparar servicios y pruebas de reglas de versiones, pagos y estados según contratos. | Acceso real y variantes reales utilizables; el trabajo preparatorio de los otros módulos todavía no cuenta como CU terminado. |
| Solicitud y cotización | Revisar integración y coordinar cambios compartidos. | Completar CU-05 y CU-06; habilitar CU-08 para solicitudes Pendientes. | Con solicitudes reales, completar CU-10 y CU-11. | Un pedido real puede configurarse, registrarse, consultarse, cotizarse y conservar sus versiones. |
| Comprobantes y pagos | Apoyar pruebas de duplicados, concurrencia y permisos. | Completar CU-07 y reflejar resultados en CU-08. | Completar CU-13 con revisión del mismo Pago y cobro directo. | Verificación única, rechazo persistido y saldo coherente. |
| Producción y entrega | Revisar el circuito y apoyar correcciones. | Terminar la consulta de los estados y saldos reales de CU-08. | Completar CU-12 y el archivo administrativo documentado. | Se respetan aprobación vigente, 50 %, revalidación por aumento y condiciones de entrega. |
| Cierre | Publicación e instalación reproducible; integrar informe. | Evidencias de móvil, escritorio y recorrido de cliente; revisar instrucciones. | Evidencias administrativas, transacciones, estados y clases finales. | Pruebas críticas ejecutadas, defectos resueltos, documentación coherente y defensa ensayada. |

Los tres actualizan su documentación y pruebas mientras construyen. La tabla define precedencias, no obliga a esperar sin trabajo a que otro termine una entrega completa.

### Paso 6 Mantener el control diario

Al comenzar cada sesión, cada integrante comunica tarea, resultado esperado y dependencia o bloqueo. Al terminar, muestra evidencia de lo que funciona y señala qué falta. Yirardy actualiza el tablero y redistribuye apoyo cuando alguien está bloqueado. Utilizar estados de trabajo Pendiente, En curso, En revisión y Terminado; son etiquetas del tablero del equipo y no estados nuevos del negocio.

En cada tarea de IA, especificar CU, secciones fuente, archivos que se pueden modificar, criterios de aceptación y comprobaciones. Un asistente implementa y otro revisa el cambio sin editarlo simultáneamente. La revisión de IA se complementa con la revisión del compañero y con pruebas ejecutadas. Mantener un registro breve de traspaso con cambio, evidencia, decisiones y pendientes para continuar entre herramientas sin depender de su memoria.
