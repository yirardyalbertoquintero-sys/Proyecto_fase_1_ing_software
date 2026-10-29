# Avance inicial de Yirardy — 28 de octubre de 2026

Este registro conserva el primer hito de preparación. El estado posterior, con migración y CU-01 implementado, está en [Registro de clientes](../fase-3/08-registro-clientes.md).

## Ubicación y alcance

Trabajo local en `C:/proyectos/Impacto_Textil_Yirardy`, rama `Yirardy_Quintero`, partiendo de la referencia local de `origin/main` en `1fbb780`. Se utilizó una carpeta de trabajo separada para preservar los archivos y cambios existentes en `C:/proyectos/Proyecto_fase_1_ing_software`.

La referencia remota no pudo actualizarse: la conexión disponible de GitHub devolvió 404 para el repositorio y Git local requirió autenticación. Por tanto, no se afirma que la referencia local sea la última publicada. Antes de enviar esta rama, recuperar el remoto, revisar los cambios nuevos e integrarlos sin sobrescribir trabajo de los compañeros. No se realizó push ni se creó una solicitud de cambios.

## Entregables disponibles

- Copia intacta del DOCX vigente y su huella SHA-256; instrucciones comunes en AGENTS.
- Modelo de once tablas y cinco vistas; correspondencia con el diccionario y justificación hasta 3FN.
- Cinco figuras SVG revisadas visualmente, código Mermaid y archivo draw.io con cinco páginas de elementos editables. El archivo draw.io conserva estructura XML; su apertura y ajuste final en el editor corresponde a la revisión del equipo.
- Arquitectura, propuesta de despliegue, contratos iniciales, decisiones, reparto y secuencia de integración.
- Prototipo SQL y script que ensaya la integridad en una base temporal.
- Solución .NET con API mínima de disponibilidad, manejo central de errores, configuración externa y tres pruebas HTTP.

## Comprobaciones efectuadas

| Comprobación | Resultado observado | Límite de lo demostrado |
| --- | --- | --- |
| SHA-256 del DOCX | `9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`; coincide con el original proporcionado. | Preservación del archivo, no una nueva aprobación del contenido. |
| `scripts/Test-Modelo.ps1` en SQL Server local | 29 comprobaciones superadas; base temporal retirada al terminar. | Restricciones, vistas y disparadores del prototipo; no todos los servicios del negocio. |
| Pruebas HTTP de la solución | 3 superadas, 0 fallidas: disponibilidad, indisponibilidad y excepción inesperada sin exponer detalles. | Usan sustituto de la consulta de disponibilidad; no equivalen a probar pedidos ni pagos por API. |
| API ejecutada con SQL Server real | HTTPS local, respuesta 200 y `estado: Disponible`; consulta de conectividad `SELECT 1`. | Conexión técnica a `master` con autenticación de Windows, sin crear tablas ni escribir datos. Certificado de desarrollo aceptado solo para esta comprobación. |
| Auditoría de paquetes, incluidas dependencias transitivas | Sin paquetes vulnerables conocidos según los orígenes consultados al ejecutarla. | Resultado de esa fecha; no garantiza ausencia de vulnerabilidades futuras. |
| Publicación de compilación Release | Paquete generado en `artifacts/publicacion`. | Generación local; no fue enviado a MonsterASP.NET. |
| Figuras SVG | Cinco figuras inspeccionadas a su tamaño de salida, con texto legible. | No se afirma una validación visual dentro de draw.io ni una renderización del código Mermaid. |

El entorno observado fue SDK .NET 10.0.402 y SQL Server 17.0.1000.7 Standard Developer Edition. La conexión de la comprobación no contiene contraseña y se configuró únicamente en el proceso local.

## Qué falta antes de dar por terminadas las fases 3 y 4

Revisión conjunta de decisiones, clases de diseño completas, prototipo navegable, mapeo de entidades y migraciones EF Core, implementación de CU, pruebas funcionales y de seguridad del sistema, ensayos de los RNF, despliegue real, mapeo RUP e informe final. No se ha calculado un porcentaje de avance funcional con base en tablas o archivos creados.

La siguiente unidad de trabajo de Yirardy es cerrar el diseño compartido y comenzar CU-01 de extremo a extremo. Dylan y Carlos ya pueden revisar el modelo y avanzar en el diseño de sus módulos siguiendo [la secuencia del equipo](../fase-3/07-flujo-equipo.md).
