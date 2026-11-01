# Instalar la versión actual en MonsterASP.NET

Guía de preparación y prueba de Yirardy. Fecha de consulta de las instrucciones del proveedor: 29 de octubre de 2026. El paquete actual contiene CU-01, CU-02, CU-04 y CU-09; todavía no realiza pedidos, pagos ni producción. Generar un paquete local no acredita que esté publicado.

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

El paquete excluye configuraciones appsettings por entorno, perfiles de publicación, certificados, archivos .env y storage. Las fotos existentes no se incluyen ni se eliminan: deben conservarse en el almacenamiento persistente del sitio y respaldarse junto con la base. No subir el repositorio entero ni la carpeta completa de la entrega.

## Crear y configurar el destino

1. Crear o abrir la cuenta, el sitio y su base **MSSQL** del proyecto. Comprobar en el panel el runtime .NET 10 y el acceso a SQL Server; no cambiar motor ni alojamiento por los ejemplos del panel.
2. Guardar las credenciales del proveedor en un gestor privado, fuera del repositorio y del chat. Un perfil .publishSettings puede contener secretos y está ignorado por Git.
3. Identificar una carpeta persistente permitida para fotos, preferiblemente fuera de la raíz web, con permisos de lectura y escritura de la aplicación. La ruta debe ser de disco del servidor, no una URL. Confirmar que la publicación no la borre.
4. En el sitio, usar **Scripting → Environment variables** y definir los valores siguientes. Reiniciar la aplicación cuando se cambien.

| Variable | Valor del proyecto |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__ImpactoTextil` | Cadena SQL real de la base del proyecto, tomada del panel; no publicarla en archivos. Verificar cifrado y certificado conforme a la conexión del proveedor. |
| `AllowedHosts` | El nombre real del subdominio del sitio, sin https:// ni ruta. |
| `Catalogo__CarpetaImagenes` | Ruta persistente de disco elegida para fotos públicas, separada de los futuros archivos privados. |
| `Sesion__MinutosInactividad` | `10`, si se explicita; es el valor ya confirmado y predeterminado. |

El panel puede variar; contrastar sus nombres con la [guía oficial de variables](https://help.monsterasp.net/websites/configuration/environment-variables). No activar Development para corregir errores de un sitio público.

## Base de datos y archivos del programa

Antes de actualizar una base con datos, obtener un respaldo y revisar el SQL. En la primera instalación, seleccionar la base MSSQL del proyecto y ejecutar `database/actualizar.sql` con la herramienta SQL autorizada del proveedor o SSMS. El archivo procede de las migraciones; no aplicar además el prototipo `001_modelo_fase3.sql`. El programa web no migra ni crea administradores automáticamente al arrancar.

Subir **solo sitio.zip** mediante la opción ZIP/Files del panel y extraer su contenido en la raíz de documentos del sitio, llamada `\wwwroot` por el proveedor. Alternativa: cargar el contenido de `app/` con el mecanismo de publicación autorizado. En el nivel de destino deben quedar web.config, ImpactoTextil.Api.dll y la carpeta wwwroot interna que contiene index.html, CSS y JS. No dejar una carpeta app intermedia ni subir únicamente el frontend. Consultar la [publicación oficial](https://help.monsterasp.net/getting-started/first-steps/publish-first-application).

Para una actualización, detener el sitio o usar el mecanismo de mantenimiento del proveedor, guardar el paquete anterior y conservar fotos/configuración persistente. No habilitar opciones que eliminen archivos del servidor sin revisar su efecto sobre esos datos.

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

Después comprobar manualmente registro, acceso/cierre, roles, catálogo y alta/edición/desactivación con una foto ficticia. Reiniciar el sitio y verificar que la foto y el producto persisten. El almacén de sesiones actual se vacía al reiniciar: volver a iniciar sesión es lo esperado. Verificar además la configuración de Data Protection del entorno IIS y registrar el comportamiento observado; no prometer sesiones compartidas entre instancias.

La descarga privada autorizada sigue pendiente de los CU de Carlos/Dylan. Probar una ruta directa denegada no acredita esa función. Tampoco una comprobación aislada acredita rendimiento de archivos, 50 usuarios concurrentes o disponibilidad mensual.

## Reversión

Conservar el paquete anterior, respaldo SQL y copia coherente de fotos. Si falla solo el programa, detener y restaurar el paquete anterior conservando almacenamiento y configuración. Si hubo migraciones, revisar compatibilidad antes de revertir código. No ejecutar DROP, migraciones descendentes ni restaurar datos automáticamente: la recuperación de base requiere una operación planificada sobre el respaldo correcto.

Registrar dirección real, commit, fecha, versión del runtime, resultado del informe de lectura, prueba de escritura/reinicio y pendientes. Mientras no se haga en el proveedor, el estado seguirá siendo **preparado y probado localmente; alojamiento pendiente**.
