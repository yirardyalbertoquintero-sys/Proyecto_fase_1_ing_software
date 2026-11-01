# Diseño inicial de la fase 3

Esta entrega desarrolla el trabajo inicial de Yirardy: fuente e instrucciones comunes, modelo relacional, normalización, arquitectura y despliegue propuesto. Es un diseño para revisión del equipo, con un prototipo SQL comprobado localmente. No constituye aprobación comercial ni acredita que los doce casos de uso estén programados.

## Documentos y diagramas

1. [Modelo relacional y correspondencia con el diccionario](01-modelo-relacional.md).
2. [Justificación hasta tercera forma normal](02-normalizacion.md).
3. [Arquitectura y contratos compartidos](03-arquitectura.md).
4. [Despliegue y comprobaciones pendientes](04-despliegue.md).
5. [Trazabilidad y reparto de implementación](05-trazabilidad.md).
6. [Decisiones técnicas y pendientes](06-decisiones.md).
7. [Secuencia de trabajo e integración del equipo](07-flujo-equipo.md).
8. [Registro de clientes implementado y evidencias](08-registro-clientes.md).
9. [Inicio y cierre de sesión, permisos y evidencias](09-acceso-sesiones.md).
10. [Administración del catálogo y creación controlada del Administrador](10-catalogo-administracion.md).
11. [Catálogo público y detalle de variantes activas](11-catalogo-publico.md).
12. [Preparación de publicación y comprobación local en Production](12-preparacion-publicacion.md).
13. [Prototipo navegable de CU-10: revisión y cotización con datos ficticios](13-prototipo-revision-pedidos.md).
14. [Clases de diseño de CU-10, firmas existentes e historial](14-diseno-clases-cu10.md).
15. [Administración conectada de pedidos, diseños, pagos y producción](15-administracion-pedidos.md).
16. [Configuración y solicitud del cliente conectadas, con bolsa estándar y archivos privados](16-solicitudes-cliente.md).

Los resultados ejecutados y sus límites están en el [registro de avance de Yirardy](../seguimiento/avance-yirardy.md).

La carpeta `diagramas` contiene código Mermaid y diagramas editables en draw.io. Los SVG sirven para incluir figuras legibles en el informe; conservan texto vectorial.

## Resultado disponible

- DOCX canónico conservado y verificado por SHA-256.
- Once tablas para el núcleo, sus historiales y la evidencia de comprobación de sublimación; cinco vistas para recuperar los atributos lógicos calculados sin duplicar decisiones ni importes.
- Prototipo `database/001_modelo_fase3.sql`, reproducible en una base temporal con `scripts/Test-Modelo.ps1`.
- API ASP.NET Core con disponibilidad, CU-01 y CU-02: registro público, acceso/cierre, permisos y formularios adaptables. Sesión de 10 minutos de inactividad, renovable, con revocación en servidor.
- CU-09: alta, consulta, edición y baja lógica de variantes; subida validada de fotos públicas y control de ediciones simultáneas. Comando interactivo para crear el primer Administrador sin credenciales predefinidas.
- CU-04: consulta pública de variantes activas, búsqueda/filtros y detalle revalidado; precios de referencia, manejo de catálogo vacío y errores de consulta o imagen.
- Entidades y migración inicial EF Core con las restricciones, vistas y disparadores del prototipo; script de distribución generado e idempotente.

Los controles SQL son parte de la integridad. Autorización, importe máximo de abono, aceptación atómica, estados, diseños y archivo ya están conectados a los servicios administrativos y comprobados por HTTP/SQL; los resultados están en el registro manual y el documento 15. El documento 16 incorpora CU-05/CU-06 y comprueba solicitudes reales visibles en administración; CU-07/CU-08 y la revisión humana del recorrido integrado permanecen pendientes. Las claves foráneas por sí solas no prueban esas reglas.

## Estado posterior de revisión

El [registro manual del 31 de octubre](../pruebas/revision-manual-administracion-20261031.md) recoge los escenarios administrativos observados, defectos corregidos y la preparación mínima de integración. La revisión cruzada humana continúa pendiente. Los informes anteriores conservan la fecha y el alcance de su construcción.

## Siguiente revisión del equipo

Dylan revisa que el modelo permita mostrar todas las pantallas previstas. Carlos revisa versiones, cotizaciones, pagos y los servicios administrativos. La migración inicial ya está preparada y probada; las observaciones posteriores deben incorporarse mediante nuevas migraciones coordinadas. Los tres revisan los contratos y las decisiones pendientes antes de integrar los siguientes módulos.

## Consolidación de diseño del 1 de noviembre

MER/3FN y clases del bloque existente se contrastaron con DbContext y código; arquitectura y despliegue ya reflejan el almacenamiento y las operaciones implementadas. Los fuentes Mermaid y exportaciones vigentes están en diagramas. El [informe de fases 3 y 4](../entrega/Informe_Fases_3_y_4_Impacto_Textil.docx) y el [mapeo RUP](../entrega/mapeo-rup.md) reúnen este corte. Completar las clases y navegación del cliente cuando se integren los avances de Dylan; no marcar el diseño de todo el sistema como aceptado todavía.
