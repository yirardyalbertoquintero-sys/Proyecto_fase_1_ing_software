# Arquitectura de la aplicación

Actualización del 31 de octubre de 2026: las operaciones administrativas de pedidos, archivos, pagos y producción están conectadas mediante PedidosAdministracionController y los servicios existentes. El contrato actual, los permisos y las pruebas están en [administración de pedidos](15-administracion-pedidos.md). Los contratos propuestos que aparecen como antecedentes deben contrastarse con ese documento al integrar los módulos de Dylan.

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

Los DTO son contratos de entrada y salida de la API. No se dibujan como una capa posterior al repositorio. El DbContext ya mapea las once tablas y las cinco vistas del diseño. CU-01 utiliza RegistrarClienteDto, ClientesController, RegistroClienteService, IUsuarioRepository/UsuarioRepository y ProtectorContrasena. CU-02 añade IniciarSesionDto, SesionController, AccesoService, ValidarSesion y AlmacenSesiones; comparte el repositorio y el verificador BCrypt. CU-09 añade GuardarProductoDto/ProductoDto, CatalogoAdministracionController, CatalogoService y ProductoRepository; ImagenCatalogoController/ImagenCatalogoStore validan y conservan fotos públicas separadas. ProductoVersion calcula la huella técnica del contenido. Los servicios de los demás CU siguen pendientes.

CU-04 reutiliza CatalogoService y el repositorio: ProductosController solicita únicamente variantes activas y devuelve ProductoPublicoDto. El filtrado de disponibilidad se aplica en SQL tanto en listado como en detalle. El DTO público omite la huella administrativa y no transmite referencias privadas o externas de imágenes.

## Sesión y permisos

La propuesta del SRS de cookie Secure y HttpOnly, mismo origen y protección antifalsificación está implementada en CU-02. El acceso verifica BCrypt y cuenta activa; las políticas Cliente y Administrador se comprueban en servidor. No se acepta un rol enviado por el navegador. La titularidad de pedidos y archivos se verificará en sus futuros servicios, sin confiar en identificadores aportados por el cliente.

Yirardy confirmó el 29 de octubre de 2026 una sesión de 10 minutos de inactividad, renovable con el uso y sin «recordarme». El ticket revocable se conserva en memoria de una instancia; reiniciar el servidor cierra las sesiones. La cuenta, el rol y la versión de credencial se revalidan por petición. La política de contraseña actualizada el 31 de octubre exige al menos 8 caracteres Unicode con mayúscula, minúscula, número y símbolo; conserva espacios permitidos, rechazo de controles y límite de 72 bytes UTF-8 de BCrypt. El acceso de cuentas anteriores se mantiene. El primer Administrador se crea mediante el comando interactivo existente, sin credenciales incorporadas a Git.

Las rutas privadas de archivos validarán rol o titularidad cada vez. Un código de pedido o una URL difícil de adivinar no sustituyen esa comprobación. La imagen pública de Producto se mantiene separada de los originales privados y comprobantes.

## Operaciones que deben ser consistentes

CU-06 valida primero datos y archivos y calcula un hash de la confirmación. Los archivos se preparan en una zona privada y se vinculan a pedido y detalles en una transacción SQL. Si falla antes de confirmar, se retiran únicamente los archivos nuevos de esa operación. Si el resultado del commit es incierto por una caída de conexión, primero se consulta la confirmación única: no se borra un archivo que pueda pertenecer a un pedido confirmado. Una tarea de conciliación controlada podrá retirar archivos temporales sin referencia; no se promete una transacción distribuida entre SQL y sistema de archivos.

CU-10 y CU-13 deben adquirir primero el control del mismo Pedido y releer los importes dentro de la transacción. Una nueva cotización aceptada no puede quedar por debajo de lo ya verificado. Un abono no puede superar el saldo. Los reintentos de una verificación resuelta devuelven su resultado; no ejecutan otro UPDATE ni crean un Pago adicional. Los conflictos se revalidan y no se ocultan como éxitos.

CU-12 comprueba todas las prendas, versiones aprobadas y cotización vigente antes de producir. La comprobación de sublimación registra la cotización, los diseños y el importe verificado observado. Cuando cambian las versiones se comprueba nuevamente. La autorización es una acción lógica del sistema, no un control físico de la máquina.

## Contratos iniciales para acordar con Carlos

El registro, las tres operaciones de sesión y las consultas públicas y administrativas del catálogo están implementados:

| Operación propuesta | Alcance y resultado |
| --- | --- |
| POST /api/clientes | CU-01; nombre, correo, contraseña y consentimiento; devuelve id y rol Cliente, nunca hash. |
| POST /api/sesion | CU-02; valida correo y contraseña y emite cookie segura. Implementado. |
| GET /api/sesion | Identidad mínima para la interfaz; sin secretos. Implementado. |
| DELETE /api/sesion | Cierre con CSRF y revocación en servidor. Implementado. |
| GET /api/productos | CU-04; solo variantes activas y precio identificado como referencial, sin caché. Implementado. |
| GET /api/productos/{id} | CU-04; detalle reconsultado de una variante activa; 404 si no existe o fue desactivada. Implementado. |
| GET y POST /api/administracion/productos | CU-09; consulta administrativa y creación controlada. Implementado. |
| GET y PUT /api/administracion/productos/{id} | CU-09; consulta y edición completa, incluida la disponibilidad, con huella de versión y bloqueo transaccional. Implementado. Sustituye la propuesta PATCH. |
| POST /api/imagenes-catalogo | Subida de foto pública validada; solo Administrador y CSRF. Implementado. |
| GET /api/imagenes-catalogo/{archivo} | Foto pública existente y referenciada por un Producto; sin acceso a archivos privados. Implementado. |

GET /api/seguridad/token entrega el token antifalsificación; el navegador lo conserva durante el envío y lo devuelve en X-CSRF-TOKEN, junto con una cookie técnica Secure, HttpOnly y SameSite=Strict. No se guarda el token en almacenamiento local. Los contratos de las rutas restantes deben coordinarse antes de conectar sus pantallas; no cambian los identificadores CU ni RF.

## Base ejecutable de esta entrega

`GET /api/disponibilidad` consulta si EF Core puede conectarse al SQL Server configurado. Devuelve 200 si puede hacerlo y 503 cuando no está configurado o no está disponible. No devuelve servidor, credenciales ni datos de clientes. Un manejador central devuelve un error comprensible con referencia ante una excepción inesperada.

La disponibilidad técnica prepara RNF-06, pero no mide su resultado mensual. El proceso web no ejecuta migraciones al arrancar; el script local las aplica de manera explícita antes de iniciar. CU-01 registra clientes con consentimiento y rechazo de roles enviados por el navegador. CU-02 incorpora sesión y políticas de acceso. Su aplicación a pedidos y archivos se realizará al construir sus servicios; ver [contratos y pruebas de CU-02](09-acceso-sesiones.md).

Referencias técnicas: [cookies en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [proveedor SQL Server de EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/). Las políticas comerciales proceden exclusivamente del DOCX.
