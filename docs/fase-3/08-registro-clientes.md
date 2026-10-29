# CU-01: registro de clientes — 28 de octubre de 2026

## Resultado

El registro está implementado de extremo a extremo en el entorno local. El formulario se sirve desde la misma aplicación que la API; envía los datos a SQL Server mediante un controlador, un servicio y un repositorio EF Core. La revisión de los compañeros y la aceptación académica siguen siendo pasos del equipo.

Trazabilidad: **CU-01, RF-01, HU-01, RNF-03 y el consentimiento de RNF-04**. La adaptación de esta pantalla aporta una comprobación parcial de RNF-01; no acredita todos los flujos ni todos los navegadores de RNF-08.

## Cómo ejecutarlo

```powershell
cd C:\proyectos\Proyecto_fase_1_ing_software
.\scripts\Iniciar-Local.ps1
```

Abrir **https://localhost:7105/**. El script prepara `ImpactoTextil` mediante EF Core y conserva sus datos entre ejecuciones. No se utiliza `master` para registrar clientes. Una instancia ya abierta en ese puerto debe detenerse antes de iniciar otra. El certificado es de desarrollo local; la publicación real sigue pendiente.

## Recorrido que debe poder explicar Yirardy

1. `src/frontend/index.html` presenta nombre, correo, contraseña, confirmación y consentimiento. La confirmación se valida únicamente en el navegador; no se guarda ni se envía a la API.
2. `registro.js` presenta errores junto al campo, evita envíos simultáneos y obtiene un token en `/api/seguridad/token`. Envía el JSON a `POST /api/clientes` con `X-CSRF-TOKEN` y la cookie técnica correspondiente.
3. `RegistrarClienteDto` valida también en el servidor y rechaza propiedades adicionales. Un campo `rol: Administrador` no se ignora: la solicitud se rechaza, conforme al criterio de RF-01.
4. `RegistroClienteService` recorta espacios del nombre y del correo, normaliza el correo para acceso, crea el hash BCrypt y asigna `Cliente`, cuenta activa y consentimiento con fecha UTC del servidor. No modifica la contraseña.
5. `UsuarioRepository` guarda el registro completo. La unicidad del correo en SQL Server resuelve también dos solicitudes concurrentes; el segundo intento recibe 409 sin una segunda cuenta.
6. La respuesta 201 contiene únicamente `id` y `rol`. Un fallo de persistencia devuelve un error comprensible con referencia; no confirma un registro no guardado ni devuelve credenciales, hash o detalles del servidor.

No se inicia una sesión automáticamente después del registro. El siguiente módulo es CU-02.

El [diagrama de clases del módulo en Mermaid](diagramas/clases-registro.mmd) relaciona DTO, controlador, servicio, repositorio, protección de contraseña y entidad Usuario.

## Decisiones confirmadas y límites

Yirardy confirmó expresamente un mínimo de **15 caracteres** el 28 de octubre de 2026. Se admiten espacios y no se obliga a combinar símbolos. Se cuentan caracteres Unicode; el límite de BCrypt se comprueba por bytes UTF-8, hasta 72 inclusive. Se rechazan caracteres de control. BCrypt.Net-Next 4.2.0 genera una sal aleatoria con coste 12. [Documentación del paquete](https://www.nuget.org/packages/BCrypt.Net-Next/4.2.0).

El cuerpo JSON del registro admite hasta 16 KiB. Se permiten 12 intentos por minuto y por IP observada, con respuesta 429 al superar el límite. En el alojamiento se debe revisar la configuración del proxy y reevaluar ese límite con las pruebas de concurrencia; no se marca RNF-05 como aprobado por esta implementación.

El token del formulario usa los mecanismos de [antifalsificación de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0). La cookie técnica es Secure, HttpOnly y SameSite=Strict. El token no se conserva en localStorage. La política de contenido restringe los recursos al propio origen.

El aviso describe una **demostración académica con datos ficticios**. No sustituye el aviso definitivo ni la revisión del tratamiento de datos reales que el SRS deja pendiente. La casilla se muestra sin marcar y su aceptación es obligatoria en cliente y servidor.

## Migración y autoridad del esquema

La migración `20261028165048_Inicial` incluye once tablas de negocio, treinta restricciones CHECK, cinco vistas y doce disparadores. EF añade `__EFMigrationsHistory`. Las vistas y disparadores se conservan como SQL versionado dentro de la migración; no se cargan desde el prototipo histórico al ejecutar la aplicación.

`DisenoComprobado` se mapea como entidad de unión. Las vistas no tienen clave y se usan para consultas. Las tablas con disparadores no usan OUTPUT directo al guardar; se evita la incompatibilidad descrita por el [proveedor SQL Server de EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/misc).

El esquema se obtuvo del prototipo comprobado, se completó con las restricciones que el mapeo inicial no incorporaba y se revisó mediante una migración sobre una base nueva. Las migraciones, sus complementos y el snapshot son ahora la autoridad. El script `database/002_migracion_inicial.sql` es una distribución generada. Los futuros cambios requieren otra migración coordinada; no editar silenciosamente una migración ya compartida o aplicada.

## Evidencia ejecutada

| Comprobación | Resultado |
| --- | --- |
| `scripts/Test-Registro.ps1` | 20 pruebas superadas, 0 fallidas y 0 omitidas con SQL Server real disponible. Incluye 9 pruebas rápidas y 11 de integración. |
| Reglas del prototipo sobre la migración | Las 29 comprobaciones anteriores vuelven a pasar dentro de una prueba de integración. |
| Registro válido | Rol Cliente, cuenta activa, correo normalizado, aceptación fechada y hash BCrypt verificable. La respuesta no contiene contraseña ni hash. |
| Concurrencia | Dos solicitudes simultáneas del mismo correo producen una respuesta 201 y una 409; queda una cuenta. |
| Validaciones y seguridad | Rechazo de falta de consentimiento, rol inyectado, datos inválidos, contraseña excesiva, token ausente o de otro navegador y exceso de intentos. |
| Fallo de persistencia | No se confirma creación ni se revelan conexión, contraseña o excepción SQL. |
| Script SQL de distribución | Aplicado dos veces en una base temporal: conserva 11 tablas de negocio, historial de migraciones, 5 vistas y 12 disparadores. |
| Navegador | Registro completo con datos ficticios, errores de campos vacíos y aviso de correo duplicado. |
| Vista de 320 px | Ancho de documento 305 px, con barra vertical; sin desplazamiento horizontal. Revisión visual de campos y mensajes. |
| Paquete Release | Compilado y arrancado localmente en entorno Production; HTML y JavaScript servidos coinciden con la fuente; disponibilidad 200. |
| Dependencias | Sin vulnerabilidades conocidas notificadas por la auditoría de paquetes ejecutada. |

Las bases temporales de mapeo, integración y distribución se retiraron al terminar. Se conserva `ImpactoTextil`, con una cuenta ficticia de la prueba visual. Las pruebas no utilizan datos personales reales.

Capturas: [formulario de escritorio](../evidencias/registro-escritorio.png), [formulario a 320 px](../evidencias/registro-movil-320.png), [registro exitoso](../evidencias/registro-exito.png) y [correo duplicado en móvil](../evidencias/registro-duplicado-movil.png).

## Siguiente módulo y pendientes generales

CU-02 debe añadir inicio y cierre de sesión, comprobación de cuenta activa y acceso según rol; su duración de sesión sigue pendiente de definir. Después continúan CU-09 y CU-04. Los demás servicios comerciales, el alojamiento MonsterASP.NET y las pruebas globales del sistema permanecen pendientes. Estos resultados no presentan la aplicación completa como terminada.
