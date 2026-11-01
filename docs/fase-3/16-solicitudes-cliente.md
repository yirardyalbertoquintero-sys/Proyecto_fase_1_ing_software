# CU-05 y CU-06: configurar prendas y registrar la solicitud

Corte: 1 de noviembre de 2026. Yirardy autorizó avanzar estos dos CU del reparto original de Dylan, sin esperar su publicación. Implementación en `Carlos_Borrell`; no se modifica la rama de Dylan ni main. La fuente sigue siendo el DOCX aprobado, conservado intacto. RF-04/RF-05/RF-06/RF-15 se materializan mediante carrito temporal, confirmación, archivos privados y persistencia del expediente inicial. La consulta posterior del cliente (CU-08) y sus comprobantes (CU-07) continúan pendientes.

## Recorrido desde VS Code

1. Abrir la carpeta completa del proyecto. Detener la ejecución anterior con Shift+F5 y arrancar **Impacto Textil: ejecutar aplicación** con F5.
2. Debe existir al menos una variante activa en el catálogo. Si está vacío, un Administrador debe crearla desde Gestionar catálogo; no se cargan prendas ficticias automáticamente.
3. Registrar una cuenta Cliente si todavía no existe. Entrar en **Mi sesión → Solicitar cotización**, o **Catálogo → Solicitar cotización**. El detalle de una prenda también enlaza su configuración.
4. Elegir variante y cantidad. El empaque es **Bolsa estándar**. Para una prenda personalizada, adjuntar JPG/PNG, describir la idea o ambas cosas. Para una básica, elegir **Sin personalización**. Agregar al carrito y repetir para otras configuraciones.
5. Revisar el carrito, elegir Retiro o Domicilio (con dirección obligatoria), marcar la confirmación del importe referencial y enviar.
6. Comprobar el código y el estado Pendiente. Con otra sesión de Administrador, abrir Gestionar pedidos: allí aparecen la solicitud real, sus detalles y versiones iniciales de diseño. El archivo original se descarga desde ese expediente, con los permisos existentes de CU-11.

El carrito y los objetos File permanecen únicamente en memoria de esta página. Recargar o cerrar descarta el carrito sin enviar; una solicitud ya confirmada permanece en SQL. Si vence la sesión, el acceso dentro de la misma página permite continuar conservando los archivos. Si cambia la cuenta, se oculta el carrito anterior y se exige volver a su titular o descartarlo expresamente. El recibo también se muestra únicamente a la cuenta que confirmó.

El precio mostrado es de referencia. Enviar no acepta una cotización, no aprueba un diseño, no acredita dinero y no acuerda una fecha de entrega. Incluso la versión inicial de una prenda Sin personalización se registra **Pendiente**; la aprobación corresponde al flujo administrativo.

## Decisión posterior sobre empaque

El 1 de noviembre Yirardy indicó: «TRABAJARE UNICAMENTE CON BOLSA Y NO CON ALGO PREMIUM». Para nuevas solicitudes se admite exclusivamente `Bolsa estándar`, tanto en la interfaz como en el servidor. No se añaden opciones Caja premium ni Sin empaque. Los pedidos históricos conservan sus valores; no se reescribe el DOCX aprobado ni se atribuye una confirmación documental de la propietaria que no existe. Ver D-20 en [decisiones](06-decisiones.md).

## Contrato HTTP implementado

`POST /api/solicitudes`, sesión Cliente activa, cookie segura y cabecera `X-CSRF-TOKEN`. Cuerpo `multipart/form-data` con un único campo textual `datos` (JSON) y los archivos que ese JSON identifica. No hay subida privada anticipada desde el carrito.

Ejemplo de `datos` para una configuración; los identificadores se obtienen del catálogo y de la sesión actuales:

```json
{
  "confirmacion": "41e95c36-dccb-4ca9-afbd-3a3384d9cfa0",
  "clienteConfirmado": 12,
  "modalidadEntrega": "Retiro",
  "direccionEntrega": null,
  "totalReferencialConfirmado": "300.00",
  "detalles": [{
    "producto": 8,
    "cantidad": 2,
    "talla": "M",
    "color": "Blanco",
    "tela": "Poliéster",
    "empaque": "Bolsa estándar",
    "sinPersonalizacion": false,
    "ideaDiseno": "Idea ficticia para evaluación",
    "observaciones": null,
    "archivo": "diseno-1",
    "precioBaseConfirmado": "150.00"
  }]
}
```

Adjuntar la imagen con nombre de campo `diseno-1`. Para solo idea, `archivo` es null y no se adjunta archivo; para Sin personalización, usar `sinPersonalizacion: true`, idea y archivo null. Los nombres/tokens del archivo no son rutas de almacenamiento. El servidor genera sus propias referencias privadas y conserva los bytes originales validados.

| Respuesta | Significado |
| --- | --- |
| 201 | Nueva solicitud: id, código, estado, importe referencial, fecha de registro y `esCreacionNueva: true`. |
| 200 | Reintento de la misma confirmación y contenido: mismo pedido, sin volver a guardar archivos; `esCreacionNueva: false`. |
| 400 | Datos, campos, correspondencia de adjuntos, extensión, contenido o límites inválidos. |
| 401/403 | Sesión ausente, rol no permitido, CSRF inválido o la cuenta cambió respecto a `clienteConfirmado`. |
| 409 | Confirmación reutilizada con otro contenido, o variante/características/precio cambiados; en este último caso `campo: catalogo`. |
| 413 | La petición supera el límite del servidor. |
| 5xx/fallo de red | Resultado no confirmado: conservar la misma confirmación y reintentar sin cambiar el contenido. |

`clienteConfirmado` es una precondición contra cambios de cuenta entre pestañas, no una autorización para elegir propietario. El dueño siempre se obtiene de las claims de la sesión y se contrasta en servidor. No se aceptan propiedades extra como rol, estado o cliente arbitrario.

## Consistencia y componentes

`solicitud.html/js` → `SolicitudesClienteController` → `SolicitudClienteService` → `ISolicitudClienteRepository/SolicitudClienteRepository` → EF Core/SQL Server y `IAlmacenArchivosPrivados`.

- El controlador valida el formulario completo, permisos, JSON estricto y correspondencia exacta entre líneas y adjuntos.
- El servicio calcula la huella semántica de los datos normalizados y bytes de cada imagen. La extensión debe concordar con la firma; el almacén valida además la decodificación completa. No se confía en MIME, nombre o rutas aportadas por el navegador.
- El repositorio inicia una transacción Serializable y bloquea primero al Cliente, luego las variantes en orden de id. Revalida cuenta activa, existencia, disponibilidad, talla, color, tela y precio unitario de **cada** configuración; también recalcula el total. Dos cambios de precio que se compensen en el total tampoco se aceptan silenciosamente.
- La unicidad existente `(cliente, confirmacion)` y la huella previenen duplicados y alteraciones de un reintento. Si ya se confirmó, se devuelve la fotografía original antes de rechazar cambios posteriores del catálogo. SQL puede aumentar la escala de un SUM; la respuesta normaliza a centavos para que el recibo del reintento sea legible.
- Se guardan juntos Pedido Pendiente/Activa, todos sus DetallePedido inmutables y VersionDiseno número 1 Pendiente. No se crean cotizaciones ni pagos ni versiones vigentes. No fue necesaria otra migración ni paquete.
- Si falla la operación, se revierte SQL y se intenta retirar solo sus archivos nuevos sin referencia. Si el commit fue incierto, se consulta SQL antes de borrar; ante incertidumbre se conserva el archivo y se registra el diagnóstico para conciliación. No existe una transacción distribuida entre disco y SQL ni una tarea automática de conciliación implementada.
- El navegador conserva la confirmación y los archivos de un envío dudoso, bloquea la edición y permite reintentar exactamente lo mismo. Solo vacía el carrito después de recibir el guardado confirmado. El cambio de catálogo permite actualizar, revisar y confirmar otra vez.

## Límites de diseño y comprobaciones

Se mantienen 10 000 000 bytes inclusivos por imagen JPG/PNG, según el SRS. Como presupuesto técnico local se fijan hasta 20 configuraciones, 20 000 000 bytes de archivos agregados y 20 500 000 bytes de petición multipart; JSON hasta 131 072 bytes, identificador de adjunto hasta 60 caracteres y cantidades positivas del tipo int. Estos topes no son pedidos mínimos ni tarifas comerciales; se revisarán contra cuotas y configuración reales de MonsterASP.NET. Los límites de texto reutilizan el diccionario: talla 30, color 50, tela 100, idea/observaciones 1000, dirección 250; sin controles.

Quince pruebas nuevas de integración con SQL cubren permisos/CSRF, titularidad, validaciones, registro completo, cambios de catálogo, versiones iniciales, reintentos, concurrencia, aislamiento entre clientes, límite inclusivo de archivos, rollback y fallo posterior al commit. El recorrido automatizado de Chrome usa API y SQL reales en una base aislada; únicamente se pierde deliberadamente una respuesta tras el commit para comprobar su recuperación. Ver [evidencia de esta entrega](../evidencias/solicitudes-cliente-20261101.json) y [avance y siguientes tareas](../seguimiento/avance-cliente-20261101.md).

Revisión de código por un segundo asistente realizada; revisión funcional humana del nuevo recorrido y aceptación de un compañero pendientes. No equivale a integración en main, despliegue público ni aceptación comercial.
