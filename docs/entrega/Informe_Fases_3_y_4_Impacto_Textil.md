# Diseño y construcción de Impacto Textil

Informe de las fases 3 y 4 con corte al 1 de noviembre de 2026

Universidad Nacional de Ingeniería. Área de Conocimiento de Tecnología de la Información y la Comunicación. Programa Académico de Ingeniería de Sistemas. Asignatura Ingeniería de Software I. Grupo 11, sección 3M4-SIS-S. Docente Walter José Quintero Acevedo.

Equipo: Yirardy Alberto Quintero Salamanca, Dylan Hernán Ruiz Romero y Carlos Tobías Borrell Saballos. Responsable de este corte de construcción y consolidación: Yirardy. Se conservan los aportes originales de Carlos; Dylan mantiene sus CU del cliente. Este documento fue preparado con asistencia de IA y requiere revisión del equipo. No incluye firmas ni declaraciones de originalidad atribuidas a sus integrantes.

Versión 0.1. Informe técnico para integrar posteriormente con las fases 1 y 2 aprobadas. No se presenta como informe final del equipo: faltan integración del cliente, revisión cruzada, resultados conjuntos y defensa. El documento aprobado de fases 1 y 2 permanece intacto.

## 1 Alcance y estado de la construcción

Impacto Textil gestiona pedidos de camisetas básicas y personalizadas por sublimación. La solución centraliza especificaciones, acuerdos, diseños, pagos y seguimiento; conserva WhatsApp y la atención presencial para asesoría. El alcance procede del SRS aprobado, no de herramientas de generación de código.

La rama Carlos_Borrell contiene la base publicada de Yirardy_Quintero y la continuación administrativa realizada por Yirardy. Se implementaron ocho CU con pantalla, API y persistencia local. Este estado no implica aceptación de otro integrante, integración en main ni disponibilidad para operación diaria del negocio.

| Bloque | CU | Estado comprobado |
| --- | --- | --- |
| Acceso y catálogo | 01, 02, 04 y 09 | Implementados y probados localmente. |
| Administración de pedidos | 10, 11, 13 y 12 | Pantalla, API y SQL conectados; pruebas automáticas y recorrido manual observado del líder. |
| Recorrido del cliente a cargo de Dylan | 05, 06, 07 y 08 | Publicación, revisión e integración pendientes en este corte; no se presume ausencia de trabajo local. |
| Recuperación automatizada | 03 | Diferida fuera del mínimo declarado; explicitar y acordar su diferimiento con el docente. |
| Reporte mensual | 14 | Opcional; no implementado como función de esta entrega. |

La guía docente exige al menos el 80 % de los seis CU principales conectados a una base real. Los principales son 02, 05, 06, 10, 12 y 13; este corte acredita implementación local de cuatro, equivalente al 66.67 %. Se requieren cinco para superar el umbral y la meta acordada sigue siendo completar los seis y los apoyos hasta los doce CU. No se confunde cobertura de pruebas con porcentaje de requisitos satisfechos.

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

La migración EF Core Inicial y su snapshot constituyen la autoridad del esquema. database/002_migracion_inicial.sql es su distribución idempotente generada; database/001_modelo_fase3.sql se conserva como antecedente del prototipo de diseño. No se aplican ambos como esquemas independientes sobre una misma base.

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
| Pagos | PedidosAdministracionController | PagosAdminService y DescargaComprobanteAdminService. |
| Producción y entrega | PedidosAdministracionController | ProduccionEntregaAdminService y reglas administrativas del pedido. |

Por ejemplo, Cobrar recibe RegistrarCobroDirectoDto y devuelve Task de IActionResult. Un cobro nuevo responde 201 con esCreacionNueva verdadero; repetir la misma confirmación devuelve 200 y el mismo Pago. La creación y el reintento comparten las comprobaciones de importe, referencia, fecha y saldo. El contrato completo está en docs/fase-3/15-administracion-pedidos.md.

## 6 Interfaz y funcionamiento administrativo

La navegación disponible permite registrar cuenta, iniciar sesión, consultar catálogo y, con rol Administrador, gestionar variantes y pedidos. pedidos.html consulta la API real y SQL; prototipo-pedidos.html se conserva como antecedente en memoria, sin atribuirle persistencia. Los módulos de cliente aún no integrados no se sustituyen por la herramienta de datos DEMO.

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

La revisión manual del líder, las comprobaciones automáticas y la revisión de IA son evidencias diferentes. La aceptación independiente de otro integrante sigue pendiente; la guía RC-01 a RC-06 está preparada en docs/pruebas/revision-cruzada-administracion.md. No se atribuye un reinicio del servidor ni una operación manual que no haya sido confirmada.

## 8 Mapeo de artefactos a RUP

La tabla completa se conserva en docs/entrega/mapeo-rup.md. Las fases del curso no se equiparan de forma mecánica a las fases RUP; un artefacto puede revisarse durante varias iteraciones.

| Fase RUP | Disciplina y artefactos | Estado |
| --- | --- | --- |
| Inicio | Negocio y requisitos: problema, viabilidad, alcance y backlog de fase 1. | Fuente aprobada según el líder. |
| Elaboración | Requisitos y diseño: SRS, modelos, diccionario, MER/3FN, clases, arquitectura y contratos. | Base disponible; actualizar cliente al integrar Dylan. |
| Construcción | Implementación y pruebas: código, migración, servicios, interfaz y evidencias. | Ocho CU locales; integración y revisión cruzada pendientes. |
| Transición en preparación | Despliegue, aceptación y defensa: paquete, instalación, revisión y demostración. | Ensayo local disponible; publicación real y aceptación pendientes. |
| Transversal | Gestión y configuración: Git, decisiones, reparto y trazabilidad. | Historial preservado; cambios en rama personal autorizada. |

## 9 Despliegue y preparación de integración

El destino previsto es MonsterASP.NET Free, condicionado a comprobar .NET 10, SQL Server, HTTPS, cuotas y persistencia privada. No existe evidencia de alojamiento público terminado. La guía docente permite localhost en defensa; el ensayo Production local previo no acredita compatibilidad del proveedor. La preparación del paquete es reproducible y excluye secretos, certificados, perfiles y almacenamiento persistente.

La base publicada de Yirardy está contenida en Carlos_Borrell; no se debe crear un segundo PR superpuesto. Los documentos resguardados se revisan manualmente: el mapeo RUP y la mejora de Test-Registro se reconciliaron; los textos obsoletos de contraseña de 15 caracteres, 51 pruebas y administración sin API no se restauran sobre el estado actual. El borrador de integración de pagos debe adaptarse cuando CU-07 esté disponible. El stash y el Word personal Avances y pendientes se conservan.

Antes de integrar se deben revisar CU-05/06/07/08 de Dylan, la sesión y titularidad del cliente, la solicitud atómica, sus archivos y las referencias al mismo Pago. Después se ejecutará el recorrido completo cliente→administrador y se evaluará si hacen falta migraciones. Esta consolidación no modifica el esquema ni abre PR a main.

## 10 Pendientes y condiciones de entrega

| Pendiente | Responsable o dependencia | Criterio para cerrarlo |
| --- | --- | --- |
| Revisión cruzada humana | Otro integrante disponible | Casos ejecutados, commit, observaciones y dictamen registrados. |
| CU-05/06/07/08 e integración | Dylan y coordinación de Yirardy | Recorrido del cliente compatible con administración y pruebas conjuntas. |
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
