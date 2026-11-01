# Trazabilidad y reparto de construcción

Esta tabla vincula el diseño con los requisitos, sin confundir modelo preparado con funcionalidad terminada.

| CU y RF | Responsable de construcción | Datos principales | Estado de esta entrega |
| --- | --- | --- | --- |
| CU-01 / RF-01 | Yirardy | Usuario, consentimiento y hash | Implementado y probado localmente: formulario, API, BCrypt, consentimiento y duplicados. Revisión del equipo pendiente. |
| CU-02 / RF-02 | Yirardy | Usuario, sesión y permisos | Implementado y probado localmente: BCrypt, cuenta activa, cookies seguras, CSRF, roles, revocación y caducidad de 10 minutos. Revisión de Dylan e integración en main pendientes; ver 09-acceso-sesiones.md. |
| CU-04 / RF-03 | Yirardy | Producto activo | Implementado y probado localmente: listado público, características/foto, filtros, detalle reconsultado y precio de referencia. Inactivo: fuera del listado y 404 por ID. CU-06 ya revalida al guardar. Revisión humana pendiente. |
| CU-09 / RF-14 | Yirardy | Producto y baja lógica | Implementado y probado localmente: alta/consulta/edición, fotos públicas, permisos, concurrencia y baja lógica; conserva detalles y precios históricos. CU-04 excluye inactivos y CU-06 revalida dentro de la operación. Revisión humana pendiente. |
| CU-05 y CU-06 / RF-04, RF-05, RF-06 y RF-15 | Yirardy adelanta la asignación original de Dylan por autorización del 1 de noviembre | Pedido, DetallePedido, primera VersionDiseno y archivo privado | Implementados y probados localmente: carrito temporal, bolsa estándar, envío completo, titularidad, revalidación de catálogo, idempotencia, transacción y archivos; descarga desde administración comprobada. Revisión humana del nuevo recorrido e integración en main pendientes. Ver [contrato y evidencias](16-solicitudes-cliente.md). |
| CU-07 / RF-07 | Yirardy continúa la asignación original de Dylan en Dylan_Ruiz | Pago Pendiente y comprobante | Implementado localmente: pantalla/API/SQL, titularidad, JPG/PNG/PDF privados, huella/reintentos, límite inclusivo y fallos. CU-13 resuelve el mismo Pago; pendiente/rechazado no acredita dinero. Pruebas SQL/navegador completas; revisión humana pendiente. Ver [documento 17](17-pedidos-comprobantes-cliente.md). |
| CU-08 / RF-12 | Yirardy continúa la asignación original de Dylan en Dylan_Ruiz | Pedidos propios, saldo e historial | Implementado localmente: consulta exclusiva por sesión, archivo administrativo visible, fechas/importes pendientes antes de acuerdo, historial y sincronización con administración. Aislamiento, cambio de cuenta y lecturas atrasadas comprobados; revisión humana pendiente. Ver [documento 17](17-pedidos-comprobantes-cliente.md). |
| CU-10 / RF-06 y RF-08 | Yirardy, continuación del trabajo de Carlos | VersionCotizacion, DetalleCotizacion, VersionDiseno y Constancia | Implementado localmente: pantalla/API/SQL, versiones y decisiones de cotización/diseño, archivo administrativo e historial; escenarios administrativos manuales revisados por Yirardy e integración técnica con CU-06 comprobada. Revisión cruzada y recorrido humano conjunto pendientes. Ver [contrato y pruebas](15-administracion-pedidos.md). |
| CU-11 / RF-09 | Yirardy, continuación del trabajo de Carlos | Archivo original y permisos | Implementado localmente: descarga autorizada de originales y versiones, con pertenencia y validación de contenido. Probado por HTTP/SQL y descarga confirmada por Yirardy; revisión cruzada pendiente. Ver [evidencia](15-administracion-pedidos.md). |
| CU-13 / RF-07 y RF-11 | Yirardy, continuación del trabajo de Carlos | Pago, saldo y cotización vigente | Implementado localmente: comprobante privado, decisión del mismo Pago, cobro directo idempotente, saldo y concurrencia SQL. Escenarios administrativos manuales revisados por Yirardy; conexión con CU-07 comprobada por SQL/navegador. Revisión humana del recorrido cliente-administrador pendiente. Ver [evidencia](15-administracion-pedidos.md). |
| CU-12 / RF-10 y RF-11 | Yirardy, continuación del trabajo de Carlos | Estado, versiones, comprobaciones y ConstanciaEntrega | Implementado localmente: transiciones, comprobación de sublimación vigente, mínimo del 50 %, aumento aceptado y entrega con constancia/saldo cero académico. Escenarios administrativos manuales revisados por Yirardy; reflejo de estado/saldo en CU-08 comprobado. Revisión cruzada humana pendiente. Ver [evidencia](15-administracion-pedidos.md). |
| CU-03 / RF-13 | Fuera del mínimo declarado | Recuperación, token y servicio de correo | Diferido conforme al SRS; no se implementa su tabla en este prototipo. |
| CU-14 / RF-16 | Opcional | Entregas y pagos verificados por fechas distintas | El modelo conserva datos para el reporte; función pendiente. |

Resultados y límites de la revisión del líder: [registro manual, defectos y guía del compañero](../pruebas/revision-manual-administracion-20261031.md). No equivale a aceptación independiente.

Actualización posterior del 1 de noviembre: la conexión de CU-06/CU-07 con administración y el reflejo de producción/saldo en CU-08 ya se comprobaron por HTTP/SQL y navegador (documentos 16/17). Las referencias de las filas administrativas a «integración con Dylan pendiente» conservan el pendiente de revisión conjunta y aceptación humana; la conexión técnica ya está implementada. Main y despliegue siguen fuera de este cierre.

## Primera revisión cruzada

- Dylan revisa el modelo y la guía de ejecución de Yirardy, e identifica los datos necesarios para sus pantallas.
- Carlos revisa las versiones, las constancias derivadas y la regla de inmutabilidad del detalle original. Debe confirmar la correspondencia con sus clases y servicios.
- Yirardy reúne observaciones, corrige decisiones técnicas justificadas y coordina una sola migración inicial después de cerrar el diseño.

Las tareas de cada integrante se integran de forma incremental desde sus ramas. Los archivos compartidos se modifican con coordinación previa. Un asistente puede implementar y otro revisar; la aceptación del compañero y las pruebas siguen siendo necesarias.
