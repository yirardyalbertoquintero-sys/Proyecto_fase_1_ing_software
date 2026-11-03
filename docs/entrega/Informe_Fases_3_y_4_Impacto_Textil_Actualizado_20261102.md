# Diseño y construcción de Impacto Textil

Informe de las fases 3 y 4 con cierre funcional del líder al 2 de noviembre de 2026

Universidad Nacional de Ingeniería. Área de Conocimiento de Tecnología de la Información y la Comunicación. Programa Académico de Ingeniería de Sistemas. Asignatura Ingeniería de Software I. Grupo 11, sección 3M4-SIS-S. Docente Walter José Quintero Acevedo.

Equipo: Yirardy Alberto Quintero Salamanca, Dylan Hernán Ruiz Romero y Carlos Tobías Borrell Saballos. Yirardy realizó la consolidación y confirmó su revisión funcional. Se conservan los aportes y autores del historial Git; la continuación de los bloques originalmente asignados a Carlos y Dylan no se atribuye ficticiamente a ellos. Documento preparado con asistencia de IA; no incluye firmas ni dictamen independiente de Dylan.

Versión 0.2. Consolida los doce CU del mínimo declarado y la revisión funcional satisfactoria comunicada por Yirardy el 2 de noviembre. La versión 0.1 y las fases 1 y 2 aprobadas se conservan intactas. Quedan revisión independiente de Dylan, integración de este cierre por PR, consolidación académica definitiva y despliegue real; la aceptación funcional del líder no equivale a aceptación comercial.

## 1 Alcance y estado de la construcción

Impacto Textil gestiona pedidos de camisetas básicas y personalizadas por sublimación. La solución centraliza especificaciones, acuerdos, diseños, pagos y seguimiento; conserva WhatsApp y la atención presencial para asesoría. El alcance procede del SRS aprobado, no de herramientas de generación de código.

Las tres ramas personales se integraron en main mediante los PR 4, 5 y 6 el 1 de noviembre, con merge normal y conservación de commits y autorías. La corrección de posición al volver a la ventana está publicada en Yirardy_Quintero, commit c718d09; este cierre documental se prepara en esa misma rama. Los doce CU tienen pantalla, API y persistencia local. Yirardy declara satisfactoria la revisión funcional realizada por él; la revisión independiente de Dylan sigue pendiente.

| Bloque | CU | Estado comprobado |
| --- | --- | --- |
| Acceso y catálogo | 01, 02, 04 y 09 | Implementados y probados localmente. |
| Administración de pedidos | 10, 11, 13 y 12 | Pantalla, API y SQL conectados; pruebas automáticas y recorrido manual observado del líder. |
| Recorrido del cliente originalmente asignado a Dylan | 05, 06, 07 y 08 | Implementados por Yirardy con autorización del líder, integrados y comprobados. Revisión funcional del líder declarada satisfactoria. |
| Recuperación automatizada | 03 | Diferida fuera del mínimo declarado; explicitar y acordar su diferimiento con el docente. |
| Reporte mensual | 14 | Opcional; no implementado como función de esta entrega. |

La guía docente exige al menos el 80 % de seis CU principales conectados a una base real. Los seis principales 02, 05, 06, 10, 12 y 13 están implementados; representan el 100 % de ese conjunto en construcción, dentro de los doce CU previstos. Este porcentaje no mide cumplimiento de todos los RNF, calificación docente ni cobertura de pruebas.

## 2 Fuentes y trazabilidad

La fuente comercial única es docs/Impacto textil fase 1 y 2.docx, SHA-256 9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821. Las guías del docente aportan los entregables académicos: clases de diseño, MER/3FN, arquitectura/despliegue, prototipos navegables, aplicación persistente, mapeo RUP y defensa.

Las fases 1 y 2 no se rehacen: sus HU, RF, CU y diccionario se vinculan a la implementación y a la evidencia. La matriz detallada está en docs/fase-3/05-trazabilidad.md. Las decisiones posteriores de contraseña y sesión se identifican con fecha y responsable, sin modificar silenciosamente el Word aprobado.

| Origen | Construcción | Evidencia |
| --- | --- | --- |
| HU-01, RF-01, CU-01 | Formulario y ClientesController | Consentimiento, duplicados, composición de contraseña y persistencia HTTP/SQL. |
| HU-02, RF-02, CU-02 | SesionController y AccesoService | Permisos, revocación y caducidad con reloj controlado. |
| RF-03 y RF-14, CU-04 y CU-09 | Catálogo público y administrativo | Disponibilidad, foto, edición concurrente, baja lógica e historial. |
| RF-06 y RF-08, CU-10 | Revisión y versiones de acuerdos/diseños | Decisiones inmutables, cotización vigente y archivo administrativo. |
| RF-09, CU-11 | Descargas privadas de diseños | Pertenencia, permisos y coincidencia exacta de archivos. |
| RF-07 y RF-11, CU-13 | Verificación y cobro directo | Mismo Pago, reintentos y concurrencia sin doble acreditación. |
| RF-10 y RF-11, CU-12 | Producción y entrega | Anticipo, aumento, aprobación vigente y entrega con constancia. |

## 3 Arquitectura y decisiones de implementación

La aplicación usa HTML, CSS y JavaScript puro, ASP.NET Core Web API en C#, Entity Framework Core, Repository, DTO y SQL Server. El frontend se sirve desde wwwroot de la misma aplicación; el navegador se comunica con los controladores por HTTPS. Los controladores validan entrada/permisos y delegan en servicios del modelo; estos aplican reglas y utilizan repositorios, DbContext y almacenamiento de archivos.

El servicio no utiliza importes enviados por la pantalla para acreditar un saldo arbitrario. Relee el pedido, las versiones y los pagos bajo transacción. Los DTO evitan exponer entidades completas, hashes o rutas físicas. Los importes decimal se transportan como cadenas JSON y la interfaz conserva centavos al presentar las cantidades.

La sesión usa cookies Secure, HttpOnly y SameSite Strict, con token antifalsificación para escrituras. Yirardy confirmó 10 minutos de inactividad, renovables con uso y sin recordar sesión. Los tickets revocables están en memoria de una instancia: reiniciar la aplicación cierra las sesiones. Las nuevas contraseñas exigen ocho caracteres Unicode, mayúscula, minúscula, número y símbolo; máximo 72 bytes UTF-8, sin controles ni recorte; BCrypt con sal y coste 12. La política nueva no impide iniciar sesión a cuentas anteriores por su composición.

Las fotos públicas de catálogo se conservan en storage/catalogo. Diseños y comprobantes utilizan storage/privado, fuera de wwwroot, o la ruta configurable ArchivosPrivados:Carpeta. Se validan contenido real, tipo y tamaño, y las descargas administrativas comprueban rol y pertenencia. El límite individual privado es inclusivo de 10 000 000 bytes; diseños JPG/PNG y comprobantes JPG/PNG/PDF. El presupuesto técnico de decodificación de imágenes es de 12 millones de píxeles.

## 4 Diseño relacional y normalización

Las migraciones EF Core Inicial y HuellaComprobanteCliente, junto con su snapshot, constituyen la autoridad del esquema. La segunda incorpora únicamente la huella técnica nullable del comprobante, conservando los pagos históricos y directos sin rellenarla retroactivamente. database/002_migracion_inicial.sql y 003_comprobantes_cliente.sql son distribuciones generadas; database/001_modelo_fase3.sql se conserva como antecedente. Para una base nueva se aplican todas las migraciones o el SQL completo del paquete; no se aplican dos modelos independientes.

El modelo tiene once tablas: Usuario, Producto, Pedido, DetallePedido, Pago, VersionDiseno, VersionCotizacion, DetalleCotizacion, ConstanciaEntrega, ComprobacionSublimacion y DisenoComprobado. Cinco vistas reconstruyen los atributos lógicos del diccionario: vPedido, vDetallePedido, vDetalleCotizacion, vVersionCotizacion y Constancia. La última proyecta decisiones de cotización y entrega; no duplica la decisión en otra tabla genérica.

En 1FN, los detalles, líneas y diseños comprobados están en relaciones separadas, sin listas serializadas dentro de una columna. En 2FN, las relaciones con clave compuesta conservan atributos que dependen de la clave completa; DetalleCotizacion utiliza la cantidad histórica inmutable de DetallePedido en lugar de duplicarla. En 3FN, las descripciones actuales de usuarios y productos permanecen en sus tablas; los detalles conservan hechos históricos, no copias que deban actualizarse con el catálogo. Los totales, anticipos y saldos se calculan, evitando copias inconsistentes.

Los punteros a versiones vigentes deben pertenecer al mismo pedido o detalle. Una revisión crea otra versión; no sobrescribe contenidos ni decisiones resueltas. Las cardinalidades mínimas semánticas de pedido completo, cotización y comprobación se aseguran al confirmar la operación en el servicio, además de las FK. La justificación detallada, claves candidatas y dependencias están en docs/fase-3/01-modelo-relacional.md y 02-normalizacion.md.

## 5 Clases de diseño y recorrido entre componentes

Las clases se contrastaron con los archivos C# existentes. Las figuras distinguen métodos públicos y privados, tipos asíncronos y valores anulables. Las fuentes Mermaid y SVG son editables/exportables; el draw.io del modelo contiene las tres vistas del MER, arquitectura y despliegue. Las tres páginas de CU-10 conservan su diseño de aplicación, DTO e historial.

| Función | Controlador | Servicios y persistencia |
| --- | --- | --- |
| Registro y acceso | ClientesController, SesionController | RegistroClienteService, AccesoService, UsuarioRepository y ProtectorContrasena. |
| Catálogo | ProductosController, CatalogoAdministracionController, ImagenCatalogoController | CatalogoService, ProductoRepository e ImagenCatalogoStore. |
| Revisión y diseños | PedidosAdministracionController | RevisionCotizacionAdminService, DescargaDisenoAdminService y PedidoAdministracionRepository. |
| Solicitudes del cliente | SolicitudesClienteController | SolicitudClienteService y SolicitudClienteRepository; valida catálogo, identidad y confirmación. |
| Consulta y comprobantes del cliente | MisPedidosController | ComprobanteClienteService y PedidoClienteRepository; titularidad, huella y reintentos. |
| Pagos | PedidosAdministracionController | PagosAdminService y DescargaComprobanteAdminService. |
| Producción y entrega | PedidosAdministracionController | ProduccionEntregaAdminService y reglas administrativas del pedido. |

Por ejemplo, Cobrar recibe RegistrarCobroDirectoDto y devuelve Task de IActionResult. Un cobro nuevo responde 201 con esCreacionNueva verdadero; repetir la misma confirmación devuelve 200 y el mismo Pago. La creación y el reintento comparten las comprobaciones de importe, referencia, fecha y saldo. El contrato completo está en docs/fase-3/15-administracion-pedidos.md.

## 6 Interfaz y funcionamiento administrativo

La navegación conecta registro, Mi sesión, catálogo, Solicitar cotización, Mis pedidos y administración de variantes/pedidos. solicitud.html y mis-pedidos.html consumen la API real y SQL; prototipo-pedidos.html permanece como antecedente en memoria. Los pedidos DEMO pertenecen a su cuenta ficticia y no sustituyen las solicitudes reales de los Clientes de prueba.

El bloque administrativo lista pedidos y consulta su expediente; muestra cotizaciones, versiones de diseño, pagos, estado y condición administrativa. Permite proponer y decidir versiones, descargar originales, verificar el mismo comprobante pendiente o registrar un cobro directo, comprobar sublimación y avanzar hasta entrega. Las solicitudes que no prosperan se archivan conservando historial conforme al SRS.

Solo los pagos Verificados reducen el saldo. Con total C$1,000, se necesitan C$500 para iniciar producción, además del acuerdo y las aprobaciones. Si se acepta C$1,400 durante producción, el mínimo pasa a C$700; con C$500 verificados faltan C$200 antes de autorizar continuación de sublimación. El estado y los pagos anteriores se conservan. Para C$100.01, el mínimo redondeado hacia arriba es C$50.01.

La entrega académica requiere saldo cero, confirmación y fecha real. La política definitiva debe validarse con los propietarios antes del uso comercial. La autorización lógica de sublimación no controla físicamente la máquina del negocio.

## 7 Pruebas y revisión funcional

El cierre del 31 de octubre registró 92 pruebas aprobadas, cero fallidas y cero omitidas, con SQL Server real en bases temporales propias. Incluye catorce pruebas administrativas y comprobaciones de permisos, CSRF, versiones, archivos, saldos, duplicados, concurrencia y rollback. Las 29 comprobaciones del modelo forman parte de una prueba de integración, no se suman como 29 pruebas xUnit adicionales. El script Test-Registro.ps1 se reconcilió con el resguardo para usar Release, dependencias bloqueadas, informe TRX único y rechazo de omisiones. El 1 de noviembre volvió a ejecutar las 92 pruebas: todas aprobadas, ninguna fallida u omitida; el resultado figura en docs/evidencias/cierre-yirardy-20261101.json.

El cierre administrativo previo conserva trece recorridos automatizados con navegador, API y SQL, incluida respuesta perdida/reintento y lectura atrasada al cambiar de pedido. Hay evidencia de escritorio y móvil de 320 píxeles. Las veinte comprobaciones del formulario de contraseña utilizan endpoints interceptados y no se presentan como pruebas SQL adicionales.

| Pedido manual | Resultado observado por Yirardy |
| --- | --- |
| DEMO-ADMIN-01 | Acuerdo v1 de C$1,000 vigente; v2 de C$1,400 rechazada; descarga de archivos confirmada. |
| DEMO-ADMIN-02 | Verificación de C$500, aumento del total, cobertura adicional, producción, liquidación y entrega con C$1,400 verificados y saldo cero. |
| DEMO-ADMIN-03 | Archivo por desistimiento; estado Pendiente, condición Archivada y datos históricos conservados. |

DEF-01: la imagen de comprobante ficticio de 4 por 4 píxeles parecía blanca. Se incorporó una muestra legible para pedidos nuevos, rotulada sin validez; los archivos históricos no se sustituyeron. DEF-02: se corrigió el estado HTTP de cobro nuevo a 201; reintento permanece 200. Se verificaron la descarga exacta y la conservación de los tres expedientes y sus archivos al repetir la herramienta de demostración.

La revisión manual del líder, las comprobaciones automáticas y la revisión de IA son evidencias diferentes. El 2 de noviembre Yirardy comunicó que realizó las pruebas necesarias y que funcionalmente el sistema está bien; confirmó que fue el único revisor humano. Se registra su aceptación general sin inventar resultados individuales, capturas, reinicios ni una revisión de Dylan. La evidencia detallada disponible respalda el recorrido del pedido A; los casos restantes carecen de desglose individual aportado. La guía del compañero continúa disponible para una revisión independiente.

## 8 Mapeo de artefactos a RUP

La tabla completa se conserva en docs/entrega/mapeo-rup.md. Las fases del curso no se equiparan de forma mecánica a las fases RUP; un artefacto puede revisarse durante varias iteraciones.

| Fase RUP | Disciplina y artefactos | Estado |
| --- | --- | --- |
| Inicio | Negocio y requisitos: problema, viabilidad, alcance y backlog de fase 1. | Fuente aprobada según el líder. |
| Elaboración | Requisitos y diseño: SRS, modelos, diccionario, MER/3FN, clases, arquitectura y contratos. | Base contrastada, incluidos contratos de solicitudes y comprobantes del cliente de los documentos 16 y 17. |
| Construcción | Implementación y pruebas: doce CU, dos migraciones, servicios, interfaz y evidencias. | Construcción local completa y revisión funcional del líder satisfactoria; revisión independiente pendiente. |
| Transición en preparación | Despliegue, aceptación y defensa: paquete, instalación, revisión y demostración. | Ensayo local disponible; publicación real y aceptación pendientes. |
| Transversal | Gestión y configuración: Git, decisiones, reparto y trazabilidad. | Historial preservado; cambios en rama personal autorizada. |

## 9 Despliegue y preparación de integración

El destino previsto es MonsterASP.NET Free, condicionado a comprobar .NET 10, SQL Server, HTTPS, cuotas y persistencia privada. No existe evidencia de alojamiento público terminado. La guía docente permite localhost en defensa; el ensayo Production local previo no acredita compatibilidad del proveedor. La preparación del paquete es reproducible y excluye secretos, certificados, perfiles y almacenamiento persistente.

La integración inicial de las tres ramas ya se realizó por PR. Este cierre se prepara en Yirardy_Quintero a partir de la base común, conservando fuente aprobada, documentos históricos y el Word personal sin seguimiento. La revisión de documentos resguardados no restaura textos obsoletos de contraseña, pruebas o funciones pendientes.

CU-05/06/07/08 están conectados con administración, sesión, titularidad y el mismo Pago. La migración de huella ya fue incorporada y probada. La corrección visual y esta consolidación deben pasar por PR; no se requiere una migración adicional por actualizar documentos o conservar el desplazamiento. Un PR no acredita publicación en el proveedor.

## 10 Pendientes y condiciones de entrega

| Pendiente | Responsable o dependencia | Criterio para cerrarlo |
| --- | --- | --- |
| Revisión cruzada humana | Dylan, según lo comunicado por el líder | Ejecutar y registrar su dictamen independiente; la confirmación actual corresponde solo a Yirardy. |
| Integración de la corrección y del cierre | Yirardy, mediante PR a main | Incluir c718d09 y los registros actuales conservando el historial. La integración inicial de los doce CU ya se completó. |
| RNF de rendimiento y carga | Equipo, con entorno/protocolo acordado | Resultados medidos; no sustituirlos por pruebas unitarias. |
| Disponibilidad mensual | Observación tras publicación | Mes de evidencia; sigue pendiente en la defensa si no se ha observado. |
| Objetivos SMART | Equipo y administración | Muestras reales previstas en fase 1; no declarar logradas las reducciones del 60 % y 80 % por mostrar la aplicación. |
| Uso comercial y datos reales | Propietarios y equipo | Validar política de entrega, aviso, conservación y condiciones del alojamiento. |
| Informe integrador y defensa | Los tres integrantes | Incorporar fases 1 y 2, revisar contribuciones, portada/carnés, firmas veraces, presentación y demo. |

La fecha efectiva de defensa continúa pendiente de confirmación; las guías contienen calendarios distintos. El informe final debe respetar el formato docente y documentar resultados reales. No hay firmas ni autorización comercial implícitas por generar este documento.

## 11 Fuentes del informe y archivos editables

Fuente de negocio: docs/Impacto textil fase 1 y 2.docx, secciones 1.3, 3.3, 4, 5.3 a 5.6 y 6. Guías académicas: docs/guias/Plan_de_ejecucion_Ingenieria_de_Software_I.pdf, páginas 4 a 6; docs/guias/Fases_del_proyecto_explicacion_ampliada.pdf, páginas 7 a 9.

Diseño: docs/fase-3/01-modelo-relacional.md, 02-normalizacion.md, 03-arquitectura.md, 04-despliegue.md y 14-diseno-clases-cu10.md. Implementación administrativa y contrato: 15-administracion-pedidos.md. Evidencia: docs/pruebas/revision-manual-administracion-20261031.md y docs/evidencias/revision-manual-20261031, junto con los informes previos identificados por fecha. Los fuentes de código y migraciones se conservan en src/backend y las pantallas en src/frontend.

Las figuras editables vigentes se encuentran en docs/fase-3/diagramas. Impacto_Textil_Fase_3.drawio contiene MER, arquitectura y despliegue; Clases_CU10.drawio contiene aplicación, DTO e historial. Las clases de registro, acceso y catálogo tienen fuentes Mermaid y SVG; clases-administracion-operaciones amplía descargas, pagos y producción. Se muestran miembros seleccionados, no un inventario completo de cada método del repositorio. Los diagramas de versiones anteriores se recuperan desde el historial Git sin reemplazar las fuentes aprobadas de análisis.

## 12 Recorrido implementado del cliente

CU-05 mantiene en memoria de la página las configuraciones, cantidades, ideas y archivos del carrito. Las solicitudes nuevas utilizan Bolsa estándar, confirmada por el líder. Para Sin personalización no se exige imagen. Domicilio exige dirección. CU-06 revalida variantes y precios, toma el Cliente de la sesión y registra atómicamente Pedido, detalles y versiones iniciales de diseño. Una confirmación idempotente y los archivos conservados permiten reintentar una respuesta incierta sin duplicar la solicitud.

CU-07 aporta una evidencia privada JPG, PNG o PDF de hasta 10 000 000 bytes. Crea Pago Pendiente; CU-13 verifica o rechaza ese mismo registro. La huella de la solicitud original permite el reintento incluso si el administrador corrigió después referencia o fecha. Un rechazo se conserva sin acreditar dinero. No se solicitan credenciales bancarias ni se ejecutan transferencias.

CU-08 consulta únicamente pedidos propios, incluidos los archivados, y conserva historial de versiones, acuerdos, pagos y estados. Sin cotización aceptada, los importes definitivos y la fecha acordada permanecen pendientes, sin inventar un saldo cero. Los borradores y archivos se guardan en memoria por cuenta y pedido; recargar o cerrar descarta lo no enviado.

La asesoría y la conformidad del cliente sobre diseños y precios continúan por WhatsApp o presencialmente; el administrador registra las versiones y la constancia del acuerdo. El cliente consulta el estado y la descripción; no se declara implementado un editor gráfico, chat ni botones del cliente para aprobar diseños o cotizaciones. Esas alternativas se comentaron como posibles mejoras y no se agregan al alcance por este cierre.

Contratos y decisiones: docs/fase-3/16-solicitudes-cliente.md y 17-pedidos-comprobantes-cliente.md. Diagrama editable del bloque: docs/fase-3/diagramas/clases-pedidos-cliente.mmd.

## 13 Cierre de pruebas y aceptación funcional del líder

La suite completa se repitió para este cierre: 124 pruebas aprobadas, cero fallidas y cero omitidas, incluidas 86 con SQL real. Se crean y retiran exclusivamente bases temporales de la ejecución; la base local del usuario se conserva. Los resultados y la huella del TRX se registran en docs/evidencias/cierre-funcional-20261102.json. Las cifras de 92 pruebas de la sección 7 son antecedentes fechados, no el total actual.

Para la corrección de desplazamiento se aprobaron por separado 25 comprobaciones de interfaz en Edge headless, con API ficticia y eventos controlados. No son 25 pruebas SQL ni se suman como casos xUnit. Incluyen conservación de posición, carrito, File, historial abierto, permisos, errores y respuestas tardías de sesión.

El pedido A de la revisión del líder alcanzó Entregado, total vigente C$1,400 y pagos Verificados por C$1,400, con fecha real y saldo cero. Se conserva un pago Rechazado de C$700, distinto del importe de C$200 del ejemplo de la guía. Los bloqueos de anticipo insuficiente y entrega con saldo fueron comunicados por Yirardy. Las capturas y el registro detallado permanecen en el resguardo local artifacts/revision-manual-20261102; las credenciales y capturas de cuentas no se incorporan a Git.

El dictamen de Yirardy es funcionalmente satisfactorio para las pruebas que declara haber considerado necesarias. No consta un resultado individual aportado para cada prueba de archivo, segunda cuenta, catálogo, móvil o caducidad; este documento no los presenta como observaciones directas del asistente. La revisión independiente de Dylan sigue pendiente. Tampoco se acreditan carga, disponibilidad mensual, metas SMART ni operación comercial.

## 14 Corrección de posición al regresar a la ventana

Ocultar secciones durante una comprobación de acceso reducía la altura y llevaba el desplazamiento a cero. La corrección conserva temporalmente el espacio hasta terminar la consulta y restaura la posición para el recorrido autorizado. Respeta una nueva acción de desplazamiento del usuario y conserva el historial desplegado en Mis pedidos. Se mantiene la comprobación de sesión y se descartan lecturas antiguas después de cambiar de cuenta o cerrar sesión. Evidencia específica: docs/pruebas/correccion-posicion-pestana-20261101.md y su JSON asociado.
