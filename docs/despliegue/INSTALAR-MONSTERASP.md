# Instalar la versión actual en MonsterASP.NET

Guía actualizada el 2 de noviembre de 2026 y contrastada con el código y las instrucciones oficiales del proveedor. Por indicación expresa del líder, **Yirardy realizará el despliegue**. El paquete contiene los doce CU del mínimo implementado: CU-01, CU-02, CU-04, CU-05, CU-06, CU-07, CU-08, CU-09, CU-10, CU-11, CU-12 y CU-13. Incluye acceso, catálogo, solicitudes, comprobantes, consulta propia, cotizaciones/diseños, descargas administrativas y producción/entrega. CU-03 continúa diferido y CU-14 opcional sin implementar.

La interfaz es la versión anterior al rediseño retirado por Yirardy; los archivos de la aplicación coinciden con la versión integrada mediante el PR #7. Generar un paquete local no acredita publicación ni aceptación comercial. No se incluyen cuentas, pedidos, fotografías ni archivos de clientes: el SQL distribuye el esquema, no los datos locales.

## Preparar desde VS Code

Con SQL Server local disponible, ejecutar en la terminal del repositorio:

```powershell
.\scripts\Preparar-Publicacion.ps1
```

El comando restaura dependencias fijadas, ejecuta todas las pruebas con SQL real y exige cero fallos y cero omisiones. Publica en una carpeta nueva de `artifacts/entregas`, genera SQL idempotente desde EF Core y registra commit, rama, SDK, fuente aprobada, resultados y huellas SHA-256. Requiere guardar primero los cambios de construcción en Git. `-PermitirCambiosLocales` sirve únicamente para un ensayo marcado expresamente como tal.

Contenido de la entrega:

- `sitio.zip`: solo la aplicación que se subirá al sitio.
- `app/`: el mismo contenido sin comprimir; web.config y DLL en su raíz, y frontend en su wwwroot interno.
- `database/actualizar.sql`: migración idempotente, fuera del sitio público.
- `manifiesto.json`, `SHA256.csv`, `pruebas/resultado.trx` y esta guía: evidencia local, no contenido del sitio.

El paquete excluye configuraciones appsettings por entorno, perfiles de publicación, certificados, archivos .env y storage. Las fotos, diseños y comprobantes existentes no se incluyen ni se eliminan: deben conservarse en almacenamiento persistente y respaldarse junto con la base. No subir el repositorio entero ni la carpeta completa de la entrega. Si queda `NO-PUBLICAR.txt`, la preparación falló; revisar la salida y generar una entrega nueva una vez corregida la causa. No retirar manualmente ese indicador para declarar completa una preparación fallida.

## Crear y configurar el destino

1. Crear o abrir la cuenta, el sitio y su base **MSSQL** del proyecto. Comprobar en el panel el runtime .NET 10 y el acceso a SQL Server; no cambiar motor ni alojamiento por los ejemplos del panel.
2. Guardar las credenciales del proveedor en un gestor privado, fuera del repositorio y del chat. Un perfil .publishSettings puede contener secretos y está ignorado por Git.
3. Identificar **dos carpetas persistentes separadas** de disco, fuera de la raíz web y de la carpeta que se reemplaza al publicar: fotos públicas de catálogo y archivos privados de diseños/comprobantes. Comprobar permisos de lectura/escritura y que las actualizaciones no las borren. La ruta debe ser de disco del servidor, no una URL. Los archivos privados no deben tener una ruta HTTP directa.
4. En el sitio, usar **Scripting → Environment variables** y definir los valores siguientes. Reiniciar la aplicación cuando se cambien.

| Variable | Valor del proyecto |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__ImpactoTextil` | Cadena SQL real de la base del proyecto, tomada del panel; no publicarla en archivos. Verificar cifrado y certificado conforme a la conexión del proveedor. |
| `AllowedHosts` | El nombre real del subdominio del sitio, sin https:// ni ruta. |
| `Catalogo__CarpetaImagenes` | Ruta persistente de disco elegida para fotos públicas, separada de los archivos privados y de la raíz web. |
| `ArchivosPrivados__Carpeta` | Otra ruta persistente de disco, fuera de la raíz web, para diseños y comprobantes. Conservar archivos y referencias SQL conjuntamente. |
| `Sesion__MinutosInactividad` | `10`, si se explicita; es el valor ya confirmado y predeterminado. |

El panel puede variar; contrastar sus nombres con la [guía oficial de variables](https://help.monsterasp.net/websites/configuration/environment-variables), consultada el 2 de noviembre de 2026. Configurar valores reales solo en el panel del sitio. No copiar conexiones, contraseñas ni claves al paquete, al repositorio o al informe. No activar Development para corregir errores de un sitio público.

## Base de datos y archivos del programa

Antes de actualizar una base con datos, obtener un respaldo y revisar el SQL. En la primera instalación, seleccionar la base MSSQL del proyecto y ejecutar `database/actualizar.sql` con la herramienta SQL autorizada del proveedor o SSMS. El archivo procede de todas las migraciones actuales: `Inicial` y `HuellaComprobanteCliente`. Crea o actualiza el esquema según `__EFMigrationsHistory`, sin copiar cuentas ni pedidos de esta computadora. No aplicar además el prototipo `001_modelo_fase3.sql` ni volver a aplicar por separado los scripts 002/003 después del SQL completo del paquete. El programa web no migra ni crea administradores automáticamente al arrancar.

Subir **solo sitio.zip** mediante la opción ZIP/Files del panel y extraer su contenido en la raíz de documentos del sitio, llamada `\wwwroot` por el proveedor. Alternativa: cargar el contenido de `app/` con el mecanismo de publicación autorizado. En el nivel de destino deben quedar web.config, ImpactoTextil.Api.dll y la carpeta wwwroot interna que contiene index.html, CSS y JS. No dejar una carpeta app intermedia ni subir únicamente el frontend. Consultar la [publicación oficial](https://help.monsterasp.net/getting-started/first-steps/publish-first-application).

Para una actualización, detener el sitio o usar el mecanismo de mantenimiento del proveedor, guardar el paquete anterior y conservar fotos, diseños, comprobantes y configuración persistente. No habilitar opciones que eliminen archivos del servidor sin revisar su efecto sobre esos datos.

## HTTPS y primer Administrador

En el plan Free, el proveedor indica publicar primero y solicitar la activación de HTTPS mediante un ticket de soporte. La aplicación mantiene HTTPS y cookies Secure: no desactivar esos controles para permitir acceso provisional por HTTP. Una vez activo el certificado, comprobar el sitio por HTTPS y su redirección; seguir la [instrucción oficial para Free](https://help.monsterasp.net/domains-https/https/lets-encrypt).

El primer Administrador se crea desde una terminal interactiva autorizada con acceso a la base remota. Si el alojamiento no ofrece consola, puede ejecutarse la DLL publicada desde la computadora del responsable, usando temporalmente la conexión SQL remota. El comando solo crea el primero; no modifica roles ni contraseñas existentes. Ejecutar desde la carpeta `app/` del paquete:

```powershell
$conexionOculta = Read-Host 'Cadena SQL del sitio (entrada oculta)' -AsSecureString
$credencialConexion = New-Object System.Net.NetworkCredential -ArgumentList @('', $conexionOculta)
$conexionAnterior = $env:ConnectionStrings__ImpactoTextil
try {
    $env:ConnectionStrings__ImpactoTextil = $credencialConexion.Password
    dotnet .\ImpactoTextil.Api.dll --crear-administrador
} finally {
    $env:ConnectionStrings__ImpactoTextil = $conexionAnterior
    $credencialConexion = $null
    $conexionOculta.Dispose()
}
```

El comando solicitará además el nombre, correo y contraseña del Administrador. La conexión debe corresponder a la base del sitio, y el acceso remoto debe estar permitido por el proveedor. No pegar contraseñas en argumentos, conversaciones, capturas, web.config ni archivos versionados. El wrapper Crear-Administrador.ps1 habitual sigue configurando la base local; no usarlo para este destino remoto.

## Comprobar y registrar

Con HTTPS activo, ejecutar desde el repositorio, sustituyendo la dirección por el sitio real:

```powershell
.\scripts\Test-Publicacion.ps1 -UrlBase 'https://SUBDOMINIO-DEL-PROYECTO/'
```

La comprobación de lectura exige certificado válido, frontend, conexión SQL, consulta real del catálogo, denegación de rutas privadas y redirección HTTP. No omite la validación TLS. Guarda únicamente resultados y estados, sin cuerpos, cookies, tokens ni credenciales. Una URL local puede declarar su listener HTTP de prueba con `-UrlHttp`.

El script público realiza 14 comprobaciones de lectura del corte inicial; debe complementarse con los recorridos actuales. Con cuentas exclusivas de prueba, comprobar:

1. Registro, acceso/cierre, roles, catálogo y alta/edición/desactivación con una foto ficticia.
2. Solicitud personalizada con archivo o idea, solicitud Sin personalización, Bolsa estándar y domicilio con dirección obligatoria. Una segunda cuenta debe ver solo sus propios pedidos.
3. Revisión administrativa, versiones y descarga de diseño original/versiones. Proponer y registrar la decisión de cotización/diseño con la constancia del acuerdo; la asesoría y conformidad se obtienen por los canales externos previstos.
4. Comprobante JPG/PNG/PDF: descargar sus bytes, verificar o rechazar el **mismo Pago**, sin duplicarlo. Pendiente y Rechazado no reducen saldo; un cobro directo nuevo es un registro distinto. Probar reintento y rechazo de una referencia verificada duplicada.
5. Producción con aprobación vigente y anticipo acumulado del 50 %. En el ejemplo de prueba C$1,000 a C$1,400, con C$500 verificados, deben faltar C$200 antes de continuar la sublimación; se conservan estado y pagos.
6. Entrega de demostración solo con saldo cero, confirmación y fecha real; archivo administrativo conservando historial cuando corresponda.
7. Reiniciar el sitio y volver a descargar foto, diseño y comprobante, comprobando que contenido y referencias persisten. Probar también una actualización del paquete conservando las dos carpetas.
8. Acceso directo denegado a carpetas privadas y descarga denegada a otro Cliente. Verificar el máximo de 10 000 000 bytes por archivo privado; el envío de un comprobante admite una petición multipart de 10 050 000 bytes. Para una solicitud completa, comprobar además que el proveedor permita peticiones de 20 500 000 bytes, necesarias para las imágenes del carrito, cuyo máximo conjunto es de 20 000 000 bytes. El límite de cada imagen sigue siendo 10 000 000 bytes. No rebajar ni desactivar las validaciones de la aplicación para superar límites del servidor.

El almacén de sesiones actual se vacía al reiniciar: volver a iniciar sesión es lo esperado. Verificar además la configuración de Data Protection del entorno IIS y registrar el comportamiento observado; no prometer sesiones compartidas entre instancias. Estas son comprobaciones en el alojamiento, no una petición de repetir la revisión local ya terminada. Una ruta directa denegada por sí sola no acredita una descarga autorizada, y una comprobación aislada no acredita rendimiento de archivos, 50 usuarios concurrentes ni disponibilidad mensual.

## Respaldo y comprobación de restauración

Yirardy conservará, en un lugar privado, un respaldo de la base SQL y copias coordinadas de las carpetas configuradas en `Catalogo__CarpetaImagenes` y `ArchivosPrivados__Carpeta`. Registrar la fecha, versión del paquete y procedimiento; las credenciales se guardan por separado. Para obtener una copia coherente, detener las escrituras durante el respaldo o utilizar un mecanismo consistente confirmado por el proveedor.

Ensayar la restauración en un destino de prueba separado: restaurar SQL y las dos carpetas, ajustar las rutas de ese destino y verificar un expediente y sus descargas. No ensayar restauraciones sobre la base en uso. Hasta contar con ese resultado, registrar restauración como pendiente, aunque exista un archivo de respaldo.

## Reversión

Conservar el paquete anterior, respaldo SQL y copia coherente de fotos, diseños y comprobantes. Si falla solo el programa, detener y restaurar el paquete anterior conservando almacenamiento y configuración. Si hubo migraciones, revisar compatibilidad antes de revertir código; no retirar la huella de comprobantes nuevos mediante un downgrade. No ejecutar DROP, migraciones descendentes ni restaurar datos automáticamente: la recuperación de base requiere una operación planificada sobre el respaldo correcto.

Registrar dirección real, commit, fecha, versión del runtime, resultado del informe de lectura, prueba de escritura/reinicio y pendientes. Mientras no se haga en el proveedor, el estado seguirá siendo **preparado y probado localmente; alojamiento pendiente**.
