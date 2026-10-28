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

Los resultados ejecutados y sus límites están en el [registro de avance de Yirardy](../seguimiento/avance-yirardy.md).

La carpeta `diagramas` contiene código Mermaid y diagramas editables en draw.io. Los SVG sirven para incluir figuras legibles en el informe; conservan texto vectorial.

## Resultado disponible

- DOCX canónico conservado y verificado por SHA-256.
- Once tablas para el núcleo, sus historiales y la evidencia de comprobación de sublimación; cinco vistas para recuperar los atributos lógicos calculados sin duplicar decisiones ni importes.
- Prototipo `database/001_modelo_fase3.sql`, reproducible en una base temporal con `scripts/Test-Modelo.ps1`.
- Base ASP.NET Core con un endpoint de disponibilidad que consulta SQL Server y maneja fallos sin revelar datos internos.

Los controles del prototipo SQL son una parte de la integridad. Autorización, importe máximo de un abono, aceptación atómica de una cotización completa, transición de estados, cobertura de todos los diseños y condiciones de archivo se implementarán en los servicios correspondientes y se probarán por API. No se atribuye su cumplimiento a las claves foráneas por sí solas.

## Siguiente revisión del equipo

Dylan revisa que el modelo permita mostrar todas las pantallas previstas. Carlos revisa versiones, cotizaciones, pagos y los servicios administrativos. Los tres acuerdan los contratos y las decisiones pendientes antes de convertir el prototipo en la migración inicial de EF Core. La preparación técnica del endpoint de disponibilidad puede probarse en paralelo con esa revisión.
