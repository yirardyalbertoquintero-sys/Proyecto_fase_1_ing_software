# Modelo relacional de Impacto Textil

## Criterio de transformación

La base se deriva de Fase 2, secciones 3.1 a 3.3, 4.1 a 4.6 y RF-01 a RF-16. Conserva los nombres del dominio y los significados del diccionario. El análisis describe atributos lógicos: en el diseño, algunos se obtienen mediante vistas en lugar de columnas duplicadas. No se pierde ningún dato observable por esa decisión.

El detalle físico de columnas, tipos, nulabilidad, claves, índices y comprobaciones está en `database/001_modelo_fase3.sql`. El archivo es un prototipo de integridad para una base vacía; las migraciones definitivas corresponden a la construcción con EF Core.

## Relaciones del núcleo

| Tabla | Clave y referencias | Hecho que conserva |
| --- | --- | --- |
| Usuario | PK id; alternativa correo | Identidad, rol, habilitación, hash BCrypt y consentimiento. |
| Producto | PK id | Una variante actual de prenda, talla, color y tela, precio referencial y disponibilidad. |
| Pedido | PK id; alternativas codigo y (cliente, confirmacion); FK cliente, responsableArchivo y versionCotizacionVigente | Titular, solicitud, entrega, estado, archivo administrativo y cotización vigente. |
| DetallePedido | PK id; FK pedido, producto y versionDisenoVigente | Cantidad y especificaciones originales de una línea, diseño o idea original y precio base de referencia al confirmar. |
| Pago | PK id; alternativa (pedido, confirmacion); FK pedido y responsableVerificacion | Un cobro o evidencia con monto, fecha, medio, referencia y decisión administrativa. |

Usuario tiene cero o muchos pedidos y cada pedido tiene un cliente. Pedido tiene uno o muchos detalles al confirmarse y cero o muchos pagos. El mínimo de un detalle se verifica al completar la transacción de CU-06: una FK permite comprobar pertenencia, pero no que todo padre tenga al menos un hijo.

Producto puede aparecer en muchos detalles. Su baja es lógica en CU-09. Las referencias impiden eliminar un producto usado; el servicio aplicará la baja lógica también a productos sin pedidos. No hay eliminación en cascada de información comercial.

## Versiones y evidencia

| Tabla | Clave y referencias | Hecho que conserva |
| --- | --- | --- |
| VersionDiseno | PK id; alternativa (detalle, numeroVersion); FK detalle y responsables | Archivo o descripción de una versión y su única decisión de aprobación o rechazo. |
| VersionCotizacion | PK id; alternativa (pedido, numeroVersion); FK pedido y responsables | Identidad de una propuesta y su única decisión de aceptación o rechazo. |
| DetalleCotizacion | PK compuesta (versionCotizacion, detalle); FK a ambas y versionDiseno opcional | Precio unitario de una línea en una propuesta determinada y el diseño pertinente. |
| ConstanciaEntrega | PK id; FK pedido y responsable | Confirmación o decisión de entrega, fecha de registro y referencia del acuerdo. |
| ComprobacionSublimacion | PK id; FK versionCotizacion y responsable | Fecha y contexto de una autorización comprobada, incluido el monto verificado observado. |
| DisenoComprobado | PK compuesta (comprobacion, versionDiseno); FK a ambas | Conjunto de versiones de diseño revisadas en esa comprobación. |

Las últimas dos tablas materializan el registro exigido en 4.6: la comprobación de sublimación y su fecha se vinculan al acuerdo y a los diseños pertinentes. No añaden un caso de uso ni un estado. El servicio registra el conjunto completo en una transacción y lo vuelve a evaluar si cambian cotización o diseños. Un registro histórico nunca autoriza por sí solo una operación futura.

## Correspondencia de atributos lógicos

| Atributo del SRS | Representación del diseño |
| --- | --- |
| Pedido.totalReferencial | `vPedido`: suma de cantidad por precioBaseReferencia de los detalles originales. |
| Pedido.total | Total de `versionCotizacionVigente`, calculado desde sus líneas. NULL antes de aceptar. |
| Pedido.porcentajeAnticipo | Valor constante 50.00 en la vista y en la futura lógica de dominio. |
| Pedido.anticipoExigible | `CEILING(total * 50) / 100`; por ejemplo 100.01 genera 50.01. |
| Pedido.saldoPendiente | Total aceptado menos suma de pagos Verificados. NULL sin cotización aceptada. |
| Pedido.cotizacionAceptada | Existencia de una referencia vigente a una versión propia y Aceptada. |
| Pedido.excepcionAnticipo | Constante false; no se crea un control editable que permita activarlo. |
| DetallePedido.precioUnitarioAcordado y subtotal | `vDetallePedido`: línea correspondiente de la cotización vigente y su cálculo. |
| DetallePedido.disenoAprobado y fechaAprobacionDiseno | Versión propia Aprobada referida por versionDisenoVigente y fecha de su decisión. |
| VersionCotizacion.detalleImportes | Colección de `vDetalleCotizacion`, con detalle, cantidad histórica, precio, subtotal y versión de diseño. |
| VersionCotizacion.total | Suma de subtotales en `vVersionCotizacion`. |
| Constancia de Cotización | La vista `Constancia` proyecta la decisión inmutable de VersionCotizacion; no se guarda una segunda copia de la misma decisión. |
| Constancia de Entrega | La misma vista proyecta ConstanciaEntrega. La clave técnica de la vista es (tipo, registroOrigen). |

La cantidad histórica se conserva una vez en DetallePedido. Sus datos originales son inmutables en este MVP; las revisiones de arte se incorporan a VersionDiseno. Por ello, la colección lógica de cada cotización puede recuperar la cantidad mediante una referencia estable sin perder su fotografía histórica ni duplicar una dependencia parcial. No se implementa edición de cantidades de solicitudes existentes; no está definida como operación del MVP. Si se incorpora en el futuro, requerirá versionar también la especificación antes de habilitarla.

`precioBaseReferencia` es un campo técnico de captura del precio vigente en CU-06. Permite explicar la referencia original aun después de modificar el catálogo. Talla, color y tela originales tampoco se vuelven a leer del Producto actual al consultar el historial.

Las constancias de cobro directo se conservan en Pago.referencia, con responsable y fecha de resolución. No se amplía el conjunto de Constancia.tipo con una excepción de anticipo u otro tipo inexistente en el diccionario.

## Tipos y límites

- Identificadores: int con identidad; referencias del diccionario conservan sus nombres semánticos. Nuevas asociaciones físicas están identificadas en las tablas anteriores.
- Texto: nvarchar con las longitudes del diccionario. Los límites propuestos siguen sujetos a la confirmación que el SRS declara.
- Dinero unitario y pagos: decimal(18,2). Cálculos agregados: decimal(28,2); C# utiliza decimal. Los límites máximos de operación se validarán antes de persistir; nunca se redondea silenciosamente un importe de entrada con más de dos decimales.
- Fechas de eventos: datetimeoffset(0); almacenar el instante con zona y mostrarlo en America/Managua. Fecha de entrega acordada: date.
- Contraseñas: varchar(60) para el BCrypt estándar especificado. El CHECK de formato no verifica criptográficamente la contraseña; esa tarea corresponde a la biblioteca de hashing.
- Correo: índice único sin distinguir mayúsculas, con normalización de espacios en el servicio y rechazo de espacios exteriores en la base. La sintaxis del correo se valida en el servidor.
- Archivos: referencias privadas o públicas según el diccionario, no contenido binario en SQL ni rutas privadas expuestas al navegador.

## Integridad y transacciones pendientes de implementación

El prototipo prueba claves únicas, referencias, rangos, metadatos de decisiones, pertenencia de las versiones vigentes y preservación de versiones resueltas. Las versiones correlativas se reservarán bloqueando su pedido o detalle; no se calculará MAX(numeroVersion)+1 sin control concurrente.

Los servicios deben completar las reglas que abarcan varios registros: cliente y administradores con roles correctos; productos activos; cobertura de todas las líneas al aceptar; total no inferior a pagos recibidos; saldo no negativo; archivo solo sin acuerdos ni cobros; producción con todos los diseños aprobados; entrega bajo la política documentada. No basta con ejecutar INSERT o UPDATE sobre una tabla.

Para pagos y nueva cotización, bloquear primero el Pedido de la operación dentro de una transacción y después releer cotización, pagos y versiones. Conservar ese orden en todos los servicios, confirmar una sola vez y manejar conflictos con respuesta comprensible. `controlConcurrencia` permite detectar cambios de una pantalla desactualizada; no reemplaza la serialización de los cálculos agregados.

`confirmacion` y `huellaSolicitud` son apoyos técnicos a la idempotencia de CU-06. La misma clave y el mismo contenido semántico devuelven el pedido existente; la misma clave con datos distintos se rechaza. El hash incluye campos normalizados y hashes de archivos, no el separador aleatorio de multipart. La unicidad se limita al cliente para evitar mezclar confirmaciones de usuarios distintos.

La referencia de un pago Verificado tiene unicidad por pedido y medio, complementada por la revisión de la evidencia. No se presume que todas las entidades bancarias tengan referencias globalmente únicas.
