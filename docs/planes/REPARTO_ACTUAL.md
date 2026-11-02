# Actualización del reparto de trabajo

Fecha: 30 de octubre de 2026. Por indicación expresa de Yirardy, se actualiza la responsabilidad de implementación debido a la disponibilidad de equipo. Esta actualización prevalece sobre el reparto original del [plan de fases 3 y 4](Plan_Fases_3_y_4_Impacto_Textil.md); conserva sus requisitos, alcance y secuencia de dependencias.

## Responsabilidad actual

**Cierre funcional del líder, 2 de noviembre:** Yirardy confirma las pruebas que consideró necesarias y un resultado funcional satisfactorio. Confirmó que fue el único revisor humano. Se cierran construcción local y revisión del líder para los doce CU; la revisión independiente de Dylan permanece pendiente, junto con entrega académica definitiva, publicación y aceptación comercial. El [informe actualizado 0.2](../entrega/Informe_Fases_3_y_4_Impacto_Textil_Actualizado_20261102.docx) y el [registro de cierre](../seguimiento/cierre-funcional-20261102.md) prevalecen sobre los pendientes históricos de construcción conservados debajo.

**Autorización vigente de integración, 1 de noviembre:** Yirardy decidió pasar las tres ramas a main mediante PR. Los PR #4, #5 y #6 integran las ramas Yirardy_Quintero, Carlos_Borrell y Dylan_Ruiz, en ese orden, todas con destino main y preservando historial y ramas. Main pasa a ser la base común de revisión; los cambios nuevos continúan en ramas personales. La revisión manual corresponde a Yirardy y la revisión cruzada independiente a Dylan; ambas siguen pendientes de sus resultados. Esta instrucción reemplaza los aplazamientos de PR/main conservados como antecedentes debajo. No acredita entrega final, alojamiento ni aceptación comercial. Ver [registro de integración](../seguimiento/integracion-main-20261101.md).

**Cierre posterior de implementación, 1 de noviembre:** Yirardy completó CU-07/CU-08 en `Dylan_Ruiz`, continuando la asignación original de Dylan. Los cuatro CU del cliente tienen pantalla/API/persistencia y pruebas; no se atribuye este código como trabajo programado por Dylan. [Contrato y evidencia del nuevo bloque](../fase-3/17-pedidos-comprobantes-cliente.md). Quedan revisión humana, defensa/explicación del código por el equipo, consolidación del informe/figuras y preparación de integración. Las notas de CU-07/CU-08 pendientes que siguen debajo conservan la planificación anterior. PR/main y despliegue continúan aplazados.

**Ubicación de trabajo vigente, indicación posterior del 1 de noviembre:** continuar el bloque cliente en `Dylan_Ruiz`. Se incorpora la base funcional completa de Carlos_Borrell, necesaria para CU-05/CU-06 y su conexión administrativa, sin reescribir historial. Al comprobar origin/Dylan_Ruiz solo existía el commit inicial y ningún cambio exclusivo. Carlos_Borrell conserva sus commits publicados. Yirardy continúa implementando los avances autorizados; esta operación no convierte el código existente en autoría de Dylan. CU-07/CU-08 siguen pendientes, ahora con Dylan_Ruiz como rama de continuación. PR/main/despliegue permanecen aplazados.

**Actualización posterior, 1 de noviembre:** Yirardy indicó avanzar el trabajo de Dylan para no seguir esperando. En Carlos_Borrell adelantó CU-05/CU-06 completos; ver [solicitudes del cliente](../fase-3/16-solicitudes-cliente.md). CU-07/CU-08 siguen pendientes. El reparto original de la tabla se conserva como antecedente de asignación; no atribuir este código como contribución de Dylan ni inferir que no tenga cambios locales sin publicar. Su rama no se modifica. No se autoriza PR/main/despliegue.

| Integrante | Implementación a su cargo desde este acuerdo |
| --- | --- |
| Yirardy | Sus CU-01, CU-02, CU-04 y CU-09, más la continuación de CU-10, CU-11, CU-12 y CU-13 a partir de los avances de Carlos. Mantiene coordinación de datos, arquitectura, integración y entrega. Puede trabajar expresamente en Yirardy_Quintero y Carlos_Borrell. |
| Dylan | Conserva CU-05, CU-06, CU-07 y CU-08 y la coordinación del prototipo/navegación del cliente. |
| Carlos | Se conservan sus avances previos y la rama Carlos_Borrell. Su participación posterior en revisión y defensa se coordinará con el equipo; no se atribuyen tareas o revisiones aún no realizadas. |

El nombre de una rama no sustituye la autoría de sus cambios. Conservar el historial, registrar quién realiza cada entrega y mantener una descripción veraz de las contribuciones.

## Estado posterior al recorrido administrativo

El 31 de octubre quedaron implementados localmente los ocho CU a cargo de Yirardy. Los escenarios administrativos que él recorrió, sus límites, los defectos corregidos y la guía de revisión cruzada constan en el [registro manual](../pruebas/revision-manual-administracion-20261031.md). La revisión del compañero, la integración con Dylan, main y despliegue siguen pendientes. El orden descrito debajo conserva la planificación anterior; las pruebas administrativas de pagos ya se ejecutaron con la autorización posterior.

## Orden de continuación (antecedente de planificación)

Actualización del 31 de octubre: Yirardy autorizó completar su bloque administrativo ahora, con contratos propios y pruebas HTTP/SQL de pagos, para revisarlo al final. Ese acuerdo reemplaza el aplazamiento anterior de esas tareas. Los CU de Dylan se integrarán posteriormente; main y despliegue permanecen fuera de esta construcción. Ver [implementación administrativa](../fase-3/15-administracion-pedidos.md).

1. Mantener los cuatro CU de acceso/catálogo de Yirardy y los servicios administrativos corregidos de Carlos como base.
2. La siguiente unidad administrativa será CU-10, revisión/cotización; después CU-11, descarga autorizada. Cada CU se cierra con pantalla, API, persistencia y comprobaciones pertinentes, cuando sus dependencias y contratos estén disponibles.
3. Dylan publica configuración/solicitud (CU-05/CU-06). Entonces se revisan contratos y necesidades reales de esquema. No generar migraciones por el solo hecho de cambiar de responsable.
4. Con solicitudes y cotizaciones reales, conectar CU-07/CU-13; posteriormente CU-12 y su reflejo en CU-08.

Se mantienen las decisiones del líder de aplazar contratos compartidos y migraciones hasta revisar los cambios de Dylan, las pruebas funcionales de pagos para su etapa y, después, main, despliegue e informe final. La reasignación de personas no autoriza adelantar esas acciones ni incorpora módulos nuevos al SRS.

## Trabajo entre ramas y revisión

- Una sola carpeta activa. Comprobar la rama antes de editar; resguardar y restaurar los cambios pendientes al alternar entre ambas.
- Conservar los avances originales de Carlos y las correcciones publicadas en 162064a. No reemplazar carpetas completas ni reescribir el historial.
- Coordinar Program.cs, DbContext, migraciones, dependencias y estilos con Dylan antes de integrar cambios compartidos.
- La aceptación humana debe realizarla alguien distinto de quien implementó el cambio. Si un revisor participó directamente, asignar la revisión al otro compañero según disponibilidad; no registrar una aceptación ficticia. La revisión de IA no sustituye esa aceptación.
- Esta actualización no acredita CU adicionales terminados ni cambia el documento de requisitos aprobado.

## Cierre independiente de Yirardy del 1 de noviembre

Preparados diseño contrastado, informe de fases 3 y 4, mapeo RUP y guía de revisión cruzada. El [registro de cierre](../seguimiento/cierre-tareas-yirardy-20261101.md) conserva las tareas y las dependencias externas. Otro integrante debe aceptar realizar la revisión; no se asignan ni registran revisiones humanas ficticias. La implementación de CU-05/06/07/08 sigue a cargo de Dylan. El PR, main y despliegue permanecen aplazados.
