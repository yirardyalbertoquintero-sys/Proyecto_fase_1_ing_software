# Arquitectura de la aplicación

## Componentes y responsabilidades

La aplicación mantiene HTML, CSS y JavaScript puro y ASP.NET Core Web API. Se propone publicar el frontend desde la misma aplicación para mantener un solo origen. El navegador envía HTTP al controlador; el controlador delega en servicios del modelo; los servicios aplican reglas y utilizan repositorios EF Core y almacenamiento privado. SQL Server conserva relaciones y restricciones.

| Componente | Responsabilidad | Coordinación |
| --- | --- | --- |
| Frontend | Formularios, estados de interfaz, accesibilidad y presentación; vuelve a mostrar las validaciones del servidor. | Dylan coordina la interfaz común; cada autor construye sus pantallas. |
| Controladores | Leer DTO, comprobar autenticación y autorización de entrada, llamar al servicio y devolver respuestas HTTP coherentes. | Cada responsable de CU. |
| Servicios | Reglas de cotización, versiones, pagos, producción y transacciones. | Carlos coordina las reglas administrativas; cada autor conserva responsabilidad por sus CU. |
| Repositorios y DbContext | Consultas y persistencia, sin decidir políticas comerciales desde la interfaz. | Yirardy coordina el modelo compartido y las migraciones. |
| Almacenamiento privado | Validar archivos, generar identificadores y servir descargas autorizadas. | Contrato acordado entre Dylan y Carlos; coordinación de despliegue de Yirardy. |
| SQL Server | Integridad referencial, unicidad y persistencia. | Yirardy y revisión de los tres. |

Los DTO son contratos de entrada y salida de la API. No se dibujan como una capa posterior al repositorio. La aplicación completa utiliza las clases de dominio del SRS; el pequeño DbContext inicial solo prueba conectividad y todavía no mapea esas entidades.

## Sesión y permisos

La propuesta del SRS es cookie de sesión Secure y HttpOnly, mismo origen y protección antifalsificación para operaciones que modifican datos. El servicio de acceso verificará BCrypt y cuenta activa. Los permisos se consultan en el servidor: el cliente no puede asignarse rol administrativo ni elegir el titular de un pedido ajeno.

La duración de sesión y la política de contraseña siguen pendientes de concretar en diseño, como declara 5.5. No se ha implementado una política inventada. El alta controlada del primer administrador se realizará mediante configuración o comando de inicialización sin credenciales incorporadas a Git.

Las rutas privadas de archivos validarán rol o titularidad cada vez. Un código de pedido o una URL difícil de adivinar no sustituyen esa comprobación. La imagen pública de Producto se mantiene separada de los originales privados y comprobantes.

## Operaciones que deben ser consistentes

CU-06 valida primero datos y archivos y calcula un hash de la confirmación. Los archivos se preparan en una zona privada y se vinculan a pedido y detalles en una transacción SQL. Si falla antes de confirmar, se retiran únicamente los archivos nuevos de esa operación. Si el resultado del commit es incierto por una caída de conexión, primero se consulta la confirmación única: no se borra un archivo que pueda pertenecer a un pedido confirmado. Una tarea de conciliación controlada podrá retirar archivos temporales sin referencia; no se promete una transacción distribuida entre SQL y sistema de archivos.

CU-10 y CU-13 deben adquirir primero el control del mismo Pedido y releer los importes dentro de la transacción. Una nueva cotización aceptada no puede quedar por debajo de lo ya verificado. Un abono no puede superar el saldo. Los reintentos de una verificación resuelta devuelven su resultado; no ejecutan otro UPDATE ni crean un Pago adicional. Los conflictos se revalidan y no se ocultan como éxitos.

CU-12 comprueba todas las prendas, versiones aprobadas y cotización vigente antes de producir. La comprobación de sublimación registra la cotización, los diseños y el importe verificado observado. Cuando cambian las versiones se comprueba nuevamente. La autorización es una acción lógica del sistema, no un control físico de la máquina.

## Contratos iniciales para acordar con Carlos

Las rutas siguientes son propuestas técnicas para los CU de Yirardy, no operaciones implementadas:

| Operación propuesta | Alcance y resultado |
| --- | --- |
| POST /api/clientes | CU-01; nombre, correo, contraseña y consentimiento; devuelve id y rol Cliente, nunca hash. |
| POST /api/sesiones | CU-02; valida correo y contraseña y emite cookie segura. |
| GET /api/sesiones/actual | Identidad mínima necesaria para la interfaz; sin secretos. |
| DELETE /api/sesiones/actual | Cierre de sesión previsto por la propuesta de seguridad. |
| GET /api/productos | CU-04; variantes activas y precio identificado como referencial. |
| GET /api/productos/{id} | CU-04; detalle de una variante disponible. |
| GET y POST /api/administracion/productos | CU-09; consulta administrativa y creación controlada. |
| PATCH /api/administracion/productos/{id} | CU-09; modificación o baja lógica con validación y control de concurrencia acordado. |

El mecanismo para entregar y devolver el token antifalsificación se fijará con el cliente HTTP común. La propuesta de rutas deberá acordarse antes de conectar las pantallas; no modifica los identificadores CU ni RF.

## Base ejecutable de esta entrega

`GET /api/disponibilidad` consulta si EF Core puede conectarse al SQL Server configurado. Devuelve 200 si puede hacerlo y 503 cuando no está configurado o no está disponible. No devuelve servidor, credenciales ni datos de clientes. Un manejador central devuelve un error comprensible con referencia ante una excepción inesperada.

Esta base prepara RNF-06, pero no mide la disponibilidad mensual ni acredita un CU completo. No crea bases, ejecuta migraciones ni incorpora datos de prueba al arrancar. La autorización de los endpoints de negocio se incorporará con CU-01 y CU-02.

Referencias técnicas: [cookies en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [proveedor SQL Server de EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/). Las políticas comerciales proceden exclusivamente del DOCX.
