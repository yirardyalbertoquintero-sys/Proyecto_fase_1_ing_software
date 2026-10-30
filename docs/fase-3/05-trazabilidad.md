# Trazabilidad y reparto de construcción

Esta tabla vincula el diseño con los requisitos, sin confundir modelo preparado con funcionalidad terminada.

| CU y RF | Responsable de construcción | Datos principales | Estado de esta entrega |
| --- | --- | --- | --- |
| CU-01 / RF-01 | Yirardy | Usuario, consentimiento y hash | Implementado y probado localmente: formulario, API, BCrypt, consentimiento y duplicados. Revisión del equipo pendiente. |
| CU-02 / RF-02 | Yirardy | Usuario, sesión y permisos | Implementado y probado localmente: BCrypt, cuenta activa, cookies seguras, CSRF, roles, revocación y caducidad de 10 minutos. Revisión de Dylan e integración en main pendientes; ver 09-acceso-sesiones.md. |
| CU-04 / RF-03 | Yirardy | Producto activo | Implementado y probado localmente: listado público, características/foto, filtros, detalle reconsultado y precio de referencia. Inactivo: fuera del listado y 404 por ID. CU-06 deberá revalidar al guardar la solicitud. Revisión de Dylan pendiente. |
| CU-09 / RF-14 | Yirardy | Producto y baja lógica | Implementado y probado localmente: alta/consulta/edición, fotos públicas, permisos, concurrencia y baja lógica; conserva detalles y precios históricos. CU-04 ya excluye inactivos; integrar revalidación en CU-05/CU-06. Revisión de Dylan pendiente. |
| CU-05 y CU-06 / RF-04, RF-05, RF-06 y RF-15 | Dylan | Pedido, DetallePedido, primera VersionDiseno y archivo privado | Contrato de persistencia propuesto; implementación pendiente. |
| CU-07 / RF-07 | Dylan | Pago Pendiente y comprobante | Integridad propuesta; interfaz y API pendientes. |
| CU-08 / RF-12 | Dylan | Vistas de pedidos propios y condición administrativa | Consultas del modelo preparadas; autorización e interfaz pendientes. |
| CU-10 / RF-06 y RF-08 | Carlos | VersionCotizacion, DetalleCotizacion, VersionDiseno y Constancia | Servicios y repositorio preparatorios presentes en esta rama Carlos_Borrell. Lectura de expediente protegida y formato monetario corregido; API, pantalla y pruebas funcionales pendientes. |
| CU-11 / RF-09 | Carlos | Archivo original y permisos | Servicio y almacén privado presentes; validación completa de imágenes/PDF y conservación de originales comprobadas. Sin endpoint/pantalla de descarga; autorización de extremo a extremo pendiente. |
| CU-13 / RF-07 y RF-11 | Carlos con apoyo de Yirardy | Pago, saldo y cotización vigente | Servicio y control transaccional preparatorios presentes; no se ejecutaron pruebas funcionales de pagos en este cierre. API, pantalla y comprobación SQL/concurrencia pendientes. |
| CU-12 / RF-10 y RF-11 | Carlos | Estado, versiones, comprobaciones y ConstanciaEntrega | Servicio preparatorio presente; conserva el mínimo del 50 % y los cuatro estados. API, pantalla y validación funcional de producción/entrega pendientes. |
| CU-03 / RF-13 | Fuera del mínimo declarado | Recuperación, token y servicio de correo | Diferido conforme al SRS; no se implementa su tabla en este prototipo. |
| CU-14 / RF-16 | Opcional | Entregas y pagos verificados por fechas distintas | El modelo conserva datos para el reporte; función pendiente. |

## Primera revisión cruzada

- Dylan revisa el modelo y la guía de ejecución de Yirardy, e identifica los datos necesarios para sus pantallas.
- Carlos revisa las versiones, las constancias derivadas y la regla de inmutabilidad del detalle original. Debe confirmar la correspondencia con sus clases y servicios.
- Yirardy reúne observaciones, corrige decisiones técnicas justificadas y coordina una sola migración inicial después de cerrar el diseño.

Las tareas de cada integrante se integran de forma incremental desde sus ramas. Los archivos compartidos se modifican con coordinación previa. Un asistente puede implementar y otro revisar; la aceptación del compañero y las pruebas siguen siendo necesarias.
