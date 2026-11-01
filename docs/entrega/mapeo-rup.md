# Mapeo de Impacto Textil a RUP

Corte: 1 de noviembre de 2026. Reconciliado manualmente desde el borrador resguardado del 30 de octubre; no se aplicó el stash completo. Referencias: plan docente, página 4, y guía ampliada, página 7, en [guías](../guias/README.md).

Las fases del curso no equivalen automáticamente a las fases RUP. Esta tabla relaciona los artefactos según su propósito y disciplina; no atribuye al equipo ceremonias ni aprobaciones que no hayan ocurrido.

| Fase RUP | Disciplina | Artefactos y evidencia | Estado al corte |
| --- | --- | --- | --- |
| Inicio | Modelado del negocio y requisitos | Problema, entrevista, alcance, viabilidad, objetivos y backlog de la fase 1 del [DOCX aprobado](../Impacto%20textil%20fase%201%20y%202.docx). | Fuente aceptada según el líder, conservada intacta. Metas de mejora todavía por medir. |
| Inicio y Elaboración | Requisitos | SRS, RF/RNF, narrativas, reglas, diccionario y trazabilidad de la fase 2. | Contrato vigente; decisiones posteriores de contraseña y sesión registradas por separado. |
| Elaboración | Análisis y diseño | [Modelo relacional](../fase-3/01-modelo-relacional.md), [3FN](../fase-3/02-normalizacion.md), [arquitectura](../fase-3/03-arquitectura.md), [despliegue](../fase-3/04-despliegue.md), clases y prototipos. | Contraste técnico del bloque implementado realizado; consolidación del cliente con Dylan pendiente. |
| Elaboración y Construcción | Implementación de datos | Entidades, DbContext, migración Inicial, snapshot, vistas, restricciones y SQL generado. | Persistencia y reglas probadas con SQL Server. Sin nueva migración por el cierre documental. |
| Construcción | Implementación | CU-01/02/04/09 de Yirardy y continuación de CU-10/11/12/13 desde los avances de Carlos. | Ocho CU implementados localmente. El nombre Carlos_Borrell no cambia la autoría de Yirardy. Revisión cruzada e integración pendientes. |
| Construcción | Pruebas | Suite xUnit/HTTP/SQL, navegador, móvil y [recorrido manual del líder](../pruebas/revision-manual-administracion-20261031.md). | 92 pruebas aprobadas sin omisiones en el cierre registrado; 13 recorridos administrativos automatizados previos y escenarios manuales documentados. No acredita integración de CU-05/06/07/08. |
| Transición en preparación | Despliegue y aceptación | Scripts de paquete y comprobación, [instalación](../despliegue/INSTALAR-MONSTERASP.md) y [revisión cruzada preparada](../pruebas/revision-cruzada-administracion.md). | Production local ensayado previamente; MonsterASP.NET, aceptación del compañero y uso comercial pendientes. |
| Transversal | Gestión y configuración | Git, [reparto vigente](../planes/REPARTO_ACTUAL.md), decisiones, evidencias e [informe de fases 3 y 4](Informe_Fases_3_y_4_Impacto_Textil.md). | Historial preservado. No hay PR ni integración en main por este cierre. |

Para cerrar Transición faltan la validación del recorrido integrado, la revisión humana independiente y la preparación/ejecución del entorno de defensa. No se presenta una tabla de correspondencia como prueba de adopción completa de RUP.
