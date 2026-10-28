# Secuencia de trabajo del equipo

Las guías distinguen fase 3 (diseño) y fase 4 (construcción del prototipo funcional), con una entrega conjunta. Se puede comprobar código mientras se diseña; una API que arranca no sustituye un caso de uso completo.

## Primera ronda: acordar la base

| Orden | Yirardy | Dylan | Carlos | Resultado conjunto |
| --- | --- | --- | --- | --- |
| 1 | Presentar el DOCX vigente, AGENTS, modelo y 3FN de esta rama. | Leer sus CU y comprobar que el modelo cubre sus pantallas. | Revisar cotizaciones, pagos, diseños y producción contra el SRS. | Observaciones concretas por CU y decisión; conservar los documentos previos. |
| 2 | Coordinar claves, nombres, migraciones, sesión y configuración. | Diseñar prototipo navegable de escritorio y móvil y cliente HTTP común. | Diseñar clases, métodos, repositorios, DTO y contratos administrativos. | Modelo revisado, clases de diseño y contratos compatibles. |
| 3 | Probar el alojamiento disponible con la API mínima; documentar resultados reales. | Revisar formularios, mensajes, navegación y permisos visibles. | Revisar permisos del servidor, transacciones y archivos privados. | Lista de decisiones cerradas y pendientes, con responsables. |

Los pendientes comerciales se consultan con el propietario; los criterios académicos con el docente. Las decisiones técnicas se documentan entre los tres. No se requiere volver a aprobar todo el SRS para resolver un detalle técnico normal.

## Segunda ronda: construir por casos de uso

1. **Yirardy:** generar y probar la migración inicial del modelo acordado. Construir CU-01 (registro) y CU-02 (acceso); después CU-09 (administración de catálogo) y CU-04 (catálogo público). Cada CU incluye su pantalla, API, persistencia y pruebas. No incluir credenciales administrativas en scripts ni Git.
2. **Dylan:** mientras se prepara la sesión, construir sus pantallas navegables con datos ficticios claramente identificados. Tras integrar sesión y catálogo, completar CU-05 y CU-06 (carrito y solicitud), incluyendo archivos y confirmación sin duplicados.
3. **Carlos:** preparar clases y servicios administrativos con pruebas de reglas. Al existir solicitudes reales, completar CU-10 y CU-11 (cotización, diseños y descarga autorizada).
4. **Dylan y Carlos:** completar CU-07 y CU-13 juntos desde el punto de vista del contrato: Dylan registra evidencia Pendiente; Carlos decide sobre el mismo Pago. Yirardy revisa transacciones e integración de datos.
5. **Carlos:** completar CU-12, incluida revalidación del anticipo y diseños al continuar la sublimación. **Dylan:** completar CU-08 con consultas de pedidos propios y estado actualizado.
6. **Los tres:** ensayar el recorrido desde registro hasta entrega. Incluir C$1,000 con C$500 pagados que cambia a C$1,400: exigir C$200 adicionales antes de continuar sublimación. Probar también errores, rechazos, permisos, archivos y solicitudes repetidas.

Este orden expresa dependencias, no obliga a esperar sin trabajar: se adelantan diseño, pantallas y pruebas del propio módulo; la integración real se comprueba cuando la dependencia está disponible. CU-03 permanece diferido y CU-14 opcional conforme al documento vigente.

## Cómo integrar las ramas personales

Cada integrante conserva su rama existente. Antes de empezar, actualiza las referencias remotas y revisa qué cambió en `main`; incorpora esos cambios sin descartar su trabajo local. Al terminar un CU, deja commits pequeños, ejecuta sus pruebas y presenta una solicitud de cambios hacia `main` con CU/RF afectados, evidencia y pendientes. Otro integrante revisa; después se integra y los demás actualizan sus ramas.

Yirardy coordina `Program.cs`, DbContext, migraciones y configuración. Dylan coordina estilos y componentes de navegación. Carlos coordina contratos administrativos. Coordinar no significa que solo una persona escriba todo: se evita que dos cambios incompatibles entren al mismo archivo compartido sin revisarse.

Los asistentes de IA trabajan sobre la misma fuente vigente y una tarea delimitada. El autor humano ejecuta, revisa y explica el resultado. Para una revisión de IA usar los cambios concretos, sin permitir ediciones simultáneas sobre los mismos archivos.

## Cuándo puede marcarse un CU como terminado

- Existe correspondencia con su CU, RF y reglas vigentes.
- El recorrido funciona desde la pantalla contra la API y la base real.
- Valida entradas, autenticación, rol y titularidad en el servidor.
- Maneja fallos sin revelar excepciones ni secretos.
- Incluye pruebas significativas de éxito y errores propios del flujo; para dinero e historial, también duplicados y concurrencia.
- Otro compañero revisó el cambio y el autor puede explicarlo.
- La evidencia identifica el commit y los resultados reales; no se usa un porcentaje estimado como prueba.

## Preparación de la entrega conjunta

Los tres completan clases de diseño, MER/3FN, arquitectura y despliegue, prototipo navegable de escritorio y móvil, aplicación funcional y tabla de mapeo RUP. El plan docente exige al menos 80 % de los CU principales; el objetivo del equipo conserva los seis principales y los apoyos hasta completar los doce del mínimo propuesto.

El mapeo RUP debe distinguir artefactos de Inicio, Elaboración, Construcción y Transición, y señalar su disciplina y evidencia. Consolidar el informe de fases 1–4, historial de cambios, declaración honesta de uso de IA y originalidad, pruebas y ensayo de defensa. La fecha efectiva de entrega todavía no está confirmada.

Publicar y comprobar el sitio cuando exista acceso al alojamiento. Medir los RNF según sus criterios; la disponibilidad mensual necesita observación y no se acredita con una prueba aislada. Preparar también una demostración local reproducible y una copia de recuperación, conforme a lo permitido por la guía.
