# CU-07 y CU-08: comprobantes y consulta de pedidos propios

Construcción: 1 de noviembre de 2026, rama `Dylan_Ruiz`. Yirardy continúa las tareas originalmente asignadas a Dylan por su indicación expresa. Se conservan los commits y autorías anteriores; no se incorpora este avance a main. Fuente: DOCX aprobado, RF-07/RF-12, diccionario de Pago, reglas 3.3 y los flujos existentes CU-06/CU-13. El Word aprobado no se modifica.

## Recorrido desde VS Code

1. Detener la ejecución anterior con Shift+F5 y volver a ejecutar **Impacto Textil: ejecutar aplicación** con F5. La preparación aplica explícitamente la migración nueva a la base local antes de iniciar la API. El proceso web por sí solo no migra la base.
2. Entrar como Cliente y pulsar **Mi sesión → Mis pedidos**. También existe el enlace desde la pantalla de nueva solicitud. La lista muestra únicamente los pedidos propios, incluidos los archivados; los datos de DEMO-ADMIN pertenecen a un Cliente ficticio inactivo y no se transfieren a la cuenta recién registrada.
3. Si todavía no hay pedidos propios, crear uno mediante **Solicitar cotización**, con una variante activa del catálogo. Elegirlo en Mis pedidos: sin cotización aceptada, total, anticipo, saldo y fecha acordada aparecen pendientes de acuerdo.
4. En otra sesión o navegador, un Administrador revisa esa solicitud, cotiza y registra el acuerdo. Volver a Mis pedidos y pulsar **Actualizar mis pedidos**. La consulta recoge el mismo total, saldo, fecha y estado que administración.
5. En el pedido con cotización aceptada y condición Activa, completar **Aportar comprobante**: monto positivo con dos decimales, medio, referencia, fecha/hora del cobro y JPG/PNG/PDF de hasta 10 000 000 bytes inclusive. Se admiten los cuatro medios externos del diccionario: Efectivo, Transferencia, Depósito y Tarjeta. No se capturan credenciales bancarias ni datos de tarjetas ni se procesa el cobro.
6. Enviar: se registra un único **Pago Pendiente**. El saldo y el estado de producción no cambian. Consultar o descargar la evidencia del titular desde el expediente.
7. En administración, verificar o rechazar ese mismo Pago. Actualizar la consulta Cliente: solo Verificado reduce el saldo. Una nueva evidencia después de un rechazo crea otro registro; la decisión rechazada se conserva.

El formulario y su File se conservan solo en memoria de esta página, separados por cuenta y pedido. Cambiar entre pedidos o volver a iniciar sesión dentro de la página permite recuperarlos. Recargar/cerrar descarta el formulario sin enviar; los datos ya confirmados en SQL permanecen. Al ocultarse o restaurarse la página se limpian los datos visibles y se revalida la sesión; una respuesta atrasada de otro pedido no reemplaza al seleccionado.

## Contratos HTTP

Todas las rutas exigen Cliente activo y devuelven `Cache-Control: no-store`. No aceptan un identificador de propietario como autorización: la titularidad se obtiene de la sesión. Las escrituras requieren CSRF. Una cuenta distinta recibe el mismo 404 que un pedido inexistente, sin sus datos.

| Método/ruta | Alcance |
| --- | --- |
| GET `/api/mis-pedidos` | Lista propia, sin excluir Archivada; importes definitivos nulos antes de aceptar. |
| GET `/api/mis-pedidos/{pedidoId}` | Expediente propio: cabecera, prendas, historial de diseños/cotizaciones y pagos. DTO explícito sin rutas físicas, hashes, credenciales ni responsables internos. |
| POST `/api/mis-pedidos/{pedidoId}/comprobantes` | Multipart `datos` JSON + único archivo con campo `archivo`; 201 al crear, 200 al recuperar un reintento. |
| GET `/api/mis-pedidos/{pedidoId}/pagos/{pagoId}/comprobante` | Descarga privada del comprobante propio, `attachment` y `nosniff`, con pertenencia pedido/pago/titular. |

La interfaz envía además `X-Cliente-Confirmado` en las consultas para detectar cambios de cuenta entre comprobar la sesión y leer el expediente. Una discrepancia devuelve 403. La cabecera es una precondición adicional, no permite elegir titular. El DTO de envío incluye `clienteConfirmado`, contrastado con la sesión aun si cambia la cuenta durante el POST. Si el token antifalsificación quedó vinculado a la cuenta anterior, se revalida el acceso y se conserva el mismo envío para recuperarlo desde su titular.

Ejemplo del campo textual `datos` (identidad y pedido obtenidos de la sesión/lista actuales):

```json
{
  "confirmacion": "4cdcf952-1950-4bb3-8554-5d53927ad428",
  "clienteConfirmado": 12,
  "monto": "500.00",
  "medio": "Transferencia",
  "referencia": "Referencia ficticia del cobro",
  "fecha": "2026-10-31T16:00:00Z"
}
```

El navegador convierte la fecha/hora elegida a un instante ISO con zona. El servidor normaliza UTC a segundos. Respuesta: `pedidoId`, `pagoId`, `estadoVerificacion`, `esCreacionNueva`. No se devuelve una ruta privada. El JSON rechaza campos desconocidos; el cliente no puede enviar Verificado, responsable o fecha de revisión. Validación 400; sesión/rol 401/403; registro ajeno/inexistente 404; ausencia de acuerdo, archivo administrativo o confirmación incompatible 409; petición excesiva 413. Fallos inesperados usan el ProblemDetails general sin errores crudos.

## Persistencia, concurrencia y cambio de esquema

`MisPedidosController` → `ComprobanteClienteService` / `IPedidoClienteRepository` → `PedidoClienteRepository`, repositorio administrativo existente, EF Core y almacén privado. Las consultas de expediente reutilizan la lectura administrativa consistente, tras comprobar titularidad y proyectando un DTO propio. No se expone la respuesta administrativa completa.

El [diagrama de clases del bloque](diagramas/clases-pedidos-cliente.mmd) conserva nombres y operaciones existentes. Los sufijos `_nullable` y los guiones bajos dentro de tipos expresan nulabilidad y genéricos anidados para facilitar su lectura en Mermaid; no son clases adicionales del código. Incluye miembros seleccionados y debe incorporarse a las figuras del informe consolidado.

Se reutiliza el bloqueo del Pedido que emplean cotización, pagos y producción. Antes de guardar se releen titularidad, acuerdo y condición administrativa. El comprobante crea Pago Pendiente con campos de revisión nulos. La verificación en CU-13 actualiza ese mismo registro; su saldo se recalcula con Verificados exclusivamente. No se suman Pendientes ni se impone el 50 % a cada evidencia; el mínimo acumulado controla la producción. Un monto declarado no es dinero acreditado: CU-13 debe contrastarlo y rechaza verificar por encima del saldo.

La unicidad existente `(pedido, confirmacion)` evita duplicaciones concurrentes de la misma confirmación. El reintento exacto recupera el pago aunque su revisión ya se haya resuelto; no crea archivo ni dinero adicional. Otra confirmación con la misma evidencia semántica ya Pendiente/Verificada se rechaza para evitar la repetición accidental. La revisión administrativa sigue contrastando medio, referencia, monto y respaldo; no se presume validación bancaria automática.

**Migración `20261101194643_HuellaComprobanteCliente`:** agrega únicamente `Pago.huellaComprobante`, varbinary(32) nullable, y CHECK de longitud 32 cuando no es null. Es un apoyo técnico, no un atributo comercial nuevo ni una tabla adicional. Conserva la huella original de monto canónico, medio, referencia normalizada, fecha UTC a segundos, titular/pedido y SHA-256 del archivo. La decisión administrativa puede confirmar fecha/referencia diferentes sin destruir la identidad del envío original. La huella no se expone ni se actualiza al resolver. Una confirmación de un cobro antiguo/directo sin huella no se acepta como supuesto reintento del cliente.

Los históricos y cobros directos conservan NULL; no se recalculan huellas ni se actualizan pagos resueltos. No ejecutar un downgrade en bases con comprobantes nuevos: retiraría esa información técnica. Las once tablas y cinco vistas se mantienen, con 31 CHECK tras la actualización. Snapshot/migración y `database/003_comprobantes_cliente.sql` están alineados; el SQL es una salida idempotente generada para una base que ya tiene Inicial. Para una base nueva usar todas las migraciones o el paquete generado de publicación. No editar los scripts generados como una fuente independiente.

Los archivos conservan sus bytes originales fuera de wwwroot. Se comprueba extensión concordante, firma, integridad completa y límites. Prevalidación y almacenamiento comparten el mismo semáforo de decodificación; se mantiene el presupuesto técnico de 12 millones de píxeles. La petición multipart tiene tope técnico de 10 050 000 bytes para permitir metadatos alrededor del archivo de 10 000 000. Si falla SQL, se retira únicamente el archivo nuevo sin referencia; ante commit incierto se consulta SQL y, si no se puede comprobar, se conserva para conciliación. No hay transacción distribuida ni conciliación automática implementada.

## Validación y límites

Diecisiete nuevas pruebas SQL/HTTP cubren aislamiento, permisos, CSRF/cambio de cuenta, campos inválidos, expediente archivado, importes nulos, cuatro medios, pendiente→verificado/rechazado del mismo Pago, cambios administrativos, reintentos concurrentes, decisión posterior con referencia/fecha corregidas, comprobantes ajenos, PDF, límites inclusivos, rollback, error tras commit y actualización idempotente de una base existente con pago resuelto sin huella. El test de integridad anterior conserva sus 29 comprobaciones y reconoce ahora las dos migraciones.

La evidencia completa y las capturas se registran en [resultados](../evidencias/pedidos-cliente-20261101.json). La revisión independiente de código por otro asistente se realizó; no equivale a revisión humana ni aceptación comercial. Quedan revisión del líder y de un compañero, consolidación del informe/figuras finales, revisión del PR, alojamiento y validaciones RNF/SMART. No se ha integrado main ni se ha publicado un sitio para el negocio.
