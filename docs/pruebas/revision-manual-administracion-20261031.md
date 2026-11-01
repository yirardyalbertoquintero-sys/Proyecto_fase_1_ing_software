# Revisión del bloque administrativo — 31 de octubre de 2026

Ejecutor manual: **Yirardy Quintero**, con orientación de IA. Rama: `Carlos_Borrell`; base de revisión: `03694db`. Entorno del usuario: VS Code, HTTPS local y SQL Server. Evidencias: capturas aportadas en el chat y confirmaciones escritas; no se atribuye una revisión independiente del compañero.

## Resultados observados

| Prueba y pedido | Esperado | Observado y evidencia |
| --- | --- | --- |
| CU-10, DEMO-ADMIN-01: aceptar v1 de C$1,000 y rechazar v2 de C$1,400 | Conservar v1 vigente y ambas decisiones | Coincide; [cotizaciones](../evidencias/revision-manual-20261031/01-cotizaciones.png). |
| CU-11, DEMO-ADMIN-01: descargar original y nueva versión subida | Descargar los archivos correspondientes sin perder el historial | Confirmado por Yirardy en el chat. No se conserva aquí una captura del archivo descargado ni se atribuye una comparación de hashes manual. |
| CU-12, DEMO-ADMIN-02: iniciar sin pagos Verificados | Denegar; faltan C$500 del mínimo | Yirardy comunicó el mensaje de bloqueo. |
| CU-13, DEMO-ADMIN-02: verificar Pago 1 por C$500 | Saldo C$500; conservar el mismo pago; no iniciar producción automáticamente | Capturas del recorrido muestran Pago 1 Verificado y el resumen correcto antes del inicio. |
| CU-12, DEMO-ADMIN-02: iniciar y aceptar aumento de C$1,000 a C$1,400 | Conservar pagos/estado; exigir C$200 adicionales y nueva comprobación de sublimación | Yirardy confirmó el bloqueo; después abonó C$200, autorizó sublimación y pasó a Listo para entrega. |
| CU-12, DEMO-ADMIN-02: intentar entrega con C$700 pendientes y luego liquidar | Denegar primero; entregar con saldo cero, confirmación y fecha real | Mensaje de bloqueo aportado en el chat; [resultado Entregado, C$1,400 verificados y saldo cero](../evidencias/revision-manual-20261031/02-entregado.png). |
| CU-10, DEMO-ADMIN-03: archivar por desistimiento | Mantener Pendiente, condición Archivada, motivo, responsable, fecha e historial; retirar acciones | Coincide; [consulta posterior de Archivada e historial](../evidencias/revision-manual-20261031/03-archivada.png). |

La captura final muestra el pedido archivado dentro de la lista consultada. No acredita por sí sola una pulsación de Ctrl+R ni un reinicio del servidor. La persistencia tras recarga se comprobó además por automatización en el cierre administrativo anterior. El rechazo de una segunda versión de diseño no tuvo confirmación manual separada y no se marca aquí como ejecutado. Esto cierra los escenarios manuales observados, no la totalidad de los CU ni los RNF.

Las capturas de esta carpeta excluyen pestañas del navegador y barra del escritorio; los datos de la aplicación no se alteraron. [Procedencia y huellas](../evidencias/revision-manual-20261031/procedencia.json).

## Defectos y correcciones

**DEF-01 — Comprobante ficticio casi blanco (presentación de la demostración).** Pedido DEMO-ADMIN-02. Pasos: abrir Pagos y comprobantes → Descargar comprobante → abrir la imagen. Esperado: muestra legible para evaluar la revisión. Observado: [PNG de 4 × 4 píxeles](../evidencias/revision-manual-20261031/def01-comprobante-anterior.png), que parece blanco. La descarga no acredita un fallo del almacenamiento.

Corrección: la herramienta de demostración incorpora un [PNG de 1000 × 700](../../src/backend/ImpactoTextil.Api/Infrastructure/Muestras/comprobante-ficticio.png) con monto y referencia simulados, rotulado «SIN VALIDEZ — NO ACREDITA DINERO». Solo se aplica al crear pedidos nuevos. Repetir la tarea no reemplaza comprobantes, diseños, decisiones ni pagos existentes: el pedido que Yirardy ya verificó conservará su imagen anterior. No hay que borrar ni reiniciar su base para aplicar esta mejora.

**DEF-02 — Estado HTTP del cobro nuevo (contrato del SRS, detectado en revisión técnica).** Pasos: enviar POST de cobro directo nuevo y repetir exactamente su confirmación. SRS, texto junto a figuras 4a/4b: creación 201; actualización/reintento 200. Observado antes de corregir: ambos devolvían 200. Corrección: 201 únicamente cuando `esCreacionNueva=true`; reintento 200 con el mismo pago. Los importes, bloqueos y decisiones del servicio no cambian. Se actualizan las comprobaciones de idempotencia y concurrencia existentes.

Validación de las correcciones: [resultado de descarga y conservación](../evidencias/revision-manual-20261031/correcciones-qa.json) y [suite SQL/HTTP](../evidencias/revision-manual-20261031/suite.json). DEF-01 y DEF-02 quedan corregidos y comprobados técnicamente; la aceptación independiente del compañero permanece pendiente.

## Revisión cruzada humana — pendiente

Otro integrante que no haya implementado estos cambios debe ejecutar el proyecto con su base local propia y datos ficticios. Puede usar F5, crear su Administrador y ejecutar `Crear-PedidosDemostracion.ps1`, siguiendo [la guía administrativa](../fase-3/15-administracion-pedidos.md). No usar la base modificada de Yirardy como si fuera un escenario inicial.

Comprobar: (1) propuesta/rechazo conserva el acuerdo vigente; (2) descarga del diseño y comprobante legible; (3) un Pago Pendiente no reduce saldo y su verificación acredita una sola vez; (4) aumento 1000→1400 exige los C$200 adicionales; (5) entrega bloqueada con saldo y permitida al liquidar; (6) archivo conserva historial y bloquea modificaciones.

| Revisor | Fecha | Commit revisado | Prueba ejecutada / esperado / observado | Evidencia o defecto | Dictamen |
| --- | --- | --- | --- | --- | --- |
| Pendiente | Pendiente | Pendiente | Pendiente | Pendiente | Sin aceptación todavía |

No se ha solicitado ni enviado una revisión a otra persona. El equipo asignará al revisor disponible. La revisión del líder y la de IA son evidencias distintas de esta revisión cruzada.

## Preparación de integración — revisada, sin PR

Se consultaron las referencias de GitHub mediante `fetch origin` el 31 de octubre: `origin/Carlos_Borrell=03694db`, `origin/Yirardy_Quintero=a90020e`, `origin/Dylan_Ruiz=69a5edd` y `origin/main=8a1b321`. Carlos contiene los commits publicados de Yirardy y de la referencia de Dylan; no hay cambios exclusivos de esas dos referencias por recuperar. Esto no informa sobre el trabajo que Dylan pudiera tener sin publicar. El `main` local está atrasado; la comparación se hizo con `origin/main`.

La diferencia de base respecto de `origin/main` abarca 235 archivos: documentación, organización y aplicación; no solo cuatro CU administrativos. No preparar dos PR superpuestos de Yirardy y Carlos. Primero coordinar un único conjunto de cambios y revisar la integración del cliente. El esquema, migraciones y snapshot coinciden entre Carlos y la rama publicada de Yirardy; no se necesita una migración para estas dos correcciones.

Resguardo `2defa8cb0472167f7a92ed45877f47f4db1d5f59`, conservado sin aplicar:

| Contenido | Decisión para integrar |
| --- | --- |
| README, INDICE, arquitectura y trazabilidad | Revisados como antecedentes; conservar los actuales. El resguardo aún describe 51 pruebas, contraseña de 15 caracteres y administración sin API. No sobrescribir con esas versiones. |
| `scripts/Test-Registro.ps1` | Contiene mejoras de ejecución Release, restauración bloqueada e informe TRX; reconciliar al preparar la prueba integrada. No aplicar todo el stash. |
| `docs/entrega/mapeo-rup.md` | Borrador resguardado; consolidar cuando se conozca el flujo completo del cliente. No presentar como entrega final. |
| `docs/pruebas/integracion-pagos.md` | Borrador resguardado; las pruebas administrativas reales ya existen. Adaptar después para CU-07→CU-13 con Dylan. |
| `docs/seguimiento/cierre-base-yirardy.md` | Antecedente de la base; conservar su contexto histórico. |

El Word personal `docs/seguimiento/Avances y pendientes.docx` y `.env.vscode` quedan fuera de estos cambios. El DOCX aprobado conserva SHA-256 `9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`.

Antes del PR faltan: revisión cruzada humana; publicación/revisión de los avances de Dylan y compatibilidad de contratos; prueba cliente→administrador con solicitudes y comprobantes reales del prototipo; consolidación final de los borradores útiles. No se ha integrado a main, abierto PR, desplegado ni acreditado carga, disponibilidad mensual o medición SMART.
