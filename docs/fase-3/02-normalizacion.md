# Justificación de normalización hasta 3FN

La normalización se justifica mediante hechos, claves candidatas y dependencias funcionales. Añadir una columna id no demuestra, por sí solo, segunda ni tercera forma normal.

## Primera forma normal

Cada columna almacena un valor del dominio declarado. Las prendas de un pedido están en DetallePedido; los importes de las versiones, en DetalleCotizacion; los diseños de una comprobación, en DisenoComprobado. No se guardan esas colecciones como texto separado por comas ni como JSON dentro de Pedido. Los archivos permanecen en almacenamiento privado y las columnas conservan una referencia individual.

## Dependencias y segunda forma normal

| Relación | Claves candidatas y dependencias principales |
| --- | --- |
| Usuario | id y correo identifican una cuenta; cada clave determina nombre, hash, rol, activo y consentimiento. |
| Producto | id determina los datos de una variante actual. No se declara nombre único ni una combinación comercial única que el SRS no exige. |
| Pedido | id, codigo y (cliente, confirmacion) identifican una solicitud. Determinan sus datos de registro, entrega, condición administrativa y referencia vigente. |
| DetallePedido | id determina pedido, producto y la especificación original, incluida cantidad y precioBaseReferencia. |
| Pago | id y (pedido, confirmacion) identifican el registro. Determinan el cobro y su decisión. La referencia verificada tiene además una restricción condicional para evitar duplicados. |
| VersionDiseno | id y (detalle, numeroVersion) determinan contenido, registro y decisión de una versión. |
| VersionCotizacion | id y (pedido, numeroVersion) determinan registro y decisión de una propuesta. |
| DetalleCotizacion | (versionCotizacion, detalle) determina precioUnitarioAcordado y versionDiseno. Ningún componente aislado determina el precio de una prenda en todas sus revisiones ni las líneas de una cotización completa. |
| ConstanciaEntrega | id determina pedido, decisión, momento, responsable y referencia. |
| ComprobacionSublimacion | id determina cotización comprobada, fecha, responsable y monto observado. |
| DisenoComprobado | (comprobacion, versionDiseno) es toda la relación; no tiene atributos no clave. |

La cantidad no se duplica en DetalleCotizacion: depende del detalle original, que se mantiene inmutable. Duplicarla introduciría la dependencia parcial detalle → cantidad dentro de la clave compuesta. La vista vDetalleCotizacion reconstruye la colección lógica exigida por el SRS a partir de esa referencia histórica estable.

## Tercera forma normal

Los datos descriptivos de Usuario y Producto se almacenan en sus propias relaciones. Pedido conserva referencias a sus responsables y DetallePedido al producto; no se copian correo, nombre del administrador o el precio actual del catálogo en otras tablas.

Las especificaciones originales de DetallePedido son hechos históricos. `producto` no determina universalmente talla, color, tela o precio capturados en todos los pedidos: el catálogo puede modificarse entre solicitudes, mientras esas capturas deben permanecer. Por eso esas columnas no equivalen a duplicar la descripción actual del producto.

En ComprobacionSublimacion no se repite pedido: se obtiene desde VersionCotizacion. En DisenoComprobado no se repite detalle: se obtiene desde VersionDiseno. Se evita así almacenar dependencias transitivas de identificadores.

Las decisiones de cotización tampoco se duplican en una tabla genérica de constancias. La vista Constancia ofrece la forma lógica del diccionario mediante VersionCotizacion y ConstanciaEntrega. La primera conserva su decisión una vez; la segunda almacena un hecho distinto, la confirmación de entrega.

Totales, subtotales, anticipo y saldo se calculan en vistas. Esto reduce el riesgo de actualizar un pago y olvidar actualizar una copia del saldo. Los valores fijos porcentajeAnticipo=50 y excepcionAnticipo=false se exponen como constantes. No son columnas editables del acuerdo.

Los punteros opcionales a la versión vigente identifican una elección entre versiones del propio pedido o detalle. Las FK y las comprobaciones de pertenencia garantizan esa identidad; los servicios garantizan que la selección y las demás reglas se confirmen juntas.

## Reversibilidad de la transformación

El usuario de la aplicación sigue viendo los atributos del diccionario. `vPedido`, `vDetallePedido`, `vDetalleCotizacion`, `vVersionCotizacion` y `Constancia` reconstruyen esos datos sin alterar su significado. Los valores definitivos permanecen NULL hasta el acuerdo: no se sustituye una cotización pendiente por un total cero.

Una revisión que necesite modificar cantidades o especificaciones originales requerirá revisar el modelo y crear una nueva fotografía de especificaciones. Esa funcionalidad no se añade a esta entrega. La normalización presentada depende expresamente del contrato de inmutabilidad que protege el prototipo SQL.

## Comprobaciones efectuadas

El ensayo SQL comprueba que modificar el catálogo conserva las referencias y cotizaciones anteriores; que no puede sobrescribirse la cantidad original; que nuevas cotizaciones conservan las anteriores; y que los saldos y el anticipo se recalculan correctamente. La demostración de 3FN proviene de las dependencias expuestas; las pruebas respaldan la implementación de las decisiones que mantienen esas dependencias.
