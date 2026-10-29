# CU-02: acceso, cierre de sesión y permisos

Implementado por Yirardy en su rama `Yirardy_Quintero`, el 29 de octubre de 2026. Se aplica CU-02, RF-02 y RNF-03/RNF-04 del DOCX vigente: cuenta registrada y activa, verificación BCrypt, identidad y permisos desde servidor. No se alteró el documento aprobado ni el esquema SQL. La revisión humana de Dylan y la integración en main siguen pendientes.

## Decisión confirmada y funcionamiento

Yirardy confirmó **10 minutos de inactividad**, renovables con el uso y sin «recordarme». Se configura en `Sesion:MinutosInactividad`, actualmente 10. Cada petición autenticada válida renueva el ticket; escribir en un formulario sin enviarlo no constituye actividad del servidor. No hay sondeo periódico que mantenga la sesión abierta automáticamente.

El formulario de acceso se abre desde el registro. Consulta la sesión al cargar, permite mostrar u ocultar la contraseña y presenta errores comprensibles. Después del acceso muestra nombre y rol, sin añadir pantallas de pedidos o administración que todavía no existen. La identidad se consulta nuevamente al recargar o volver a la pestaña; un cambio de cuenta requiere cerrar la sesión actual.

El servicio normaliza el correo igual que el registro y conserva la contraseña exactamente, incluidos sus espacios. El acceso no reaplica el mínimo de creación a contraseñas existentes; sí impide superar 72 bytes antes de BCrypt. Un correo ausente, contraseña incorrecta o cuenta inactiva recibe la misma denegación y no crea sesión. Se realiza una verificación BCrypt también cuando el correo no existe; esto evita la diferencia más evidente de trabajo, sin afirmar indistinguibilidad temporal perfecta.

## Contrato compartido para Dylan y Carlos

| Ruta | Entrada / protección | Respuesta |
| --- | --- | --- |
| `GET /api/seguridad/token` | Cookie técnica y token nuevo para la identidad actual. | `200 { token }`, sin caché. |
| `POST /api/sesion` | JSON `{ correo, contrasena }`, token en `X-CSRF-TOKEN`, mismo origen y HTTPS. | `200 { id, nombre, rol }` y cookie; `400` datos/CSRF; `401` credenciales; `409` sesión ya abierta; `429` límite técnico. |
| `GET /api/sesion` | Cookie de autenticación válida. | `200 { id, nombre, rol }`; `401` sin sesión válida. |
| `DELETE /api/sesion` | Cookie válida y token CSRF de la identidad autenticada. | `204` y revocación en servidor; `400` CSRF; `401` sesión ausente. |

Los contratos anteriores de `/api/sesiones` eran propuestas; se concretan en las tres operaciones de `/api/sesion`, sin cambiar CU ni RF. Los DTO rechazan campos ajenos, incluido `rol`. Las respuestas no devuelven contraseñas, hashes ni la huella interna de credencial. Un fallo SQL devuelve el error genérico con referencia y no crea sesión.

Para llamadas que modifican datos, solicitar un token CSRF actualizado después de cambiar de identidad. El navegador usa cookies del mismo origen; no guardar tokens ni contraseñas en `localStorage` o `sessionStorage`. Un `401` solicita nuevo acceso; un `403` informa falta de permiso. Ninguno redirige una llamada API a una página HTML.

## Permisos y revocación

La cookie `__Host-ImpactoTextil.Sesion` es Secure, HttpOnly y SameSite=Strict, sin fecha de persistencia del navegador. Contiene una referencia protegida a un ticket del servidor; JavaScript no lee esa cookie. ASP.NET Core Data Protection protege su autenticidad. El token CSRF se conserva únicamente durante cada envío.

Las políticas `Cliente` y `Administrador` exigen autenticación y el rol correspondiente. Los futuros controladores deben aplicar `[Authorize(Policy = "Cliente")]` o `[Authorize(Policy = "Administrador")]`, y comprobar además la titularidad dentro del servicio. La existencia de estas políticas no acredita que los permisos sobre pedidos y archivos ya estén implementados: esos servicios aún faltan.

Cada petición autenticada relee la cuenta. Si está inactiva, cambia de rol o cambia su hash de contraseña, se invalida la sesión y se exige nuevo acceso. Salir retira el ticket del almacén; reproducir la cookie anterior no recupera la sesión. La limpieza de tickets vencidos elimina solo la versión observada, para no borrar una renovación concurrente. Una renovación posterior al cierre no resucita el ticket.

El almacén actual sirve una instancia del proceso: reiniciar la aplicación cierra todas las sesiones, incluso si el navegador conserva la cookie. Un despliegue con varias instancias requeriría un almacén compartido y coordinación de claves; comprobarlo al ensayar MonsterASP.NET. No se promete que cerrar una pestaña borre una sesión ni que todos los navegadores eliminen sus cookies si restauran una sesión previa.

El acceso admite 12 intentos por minuto por IP observada, con cuerpo JSON de hasta 16 KiB. Es un control técnico independiente del registro y no bloquea persistentemente cuentas. El proxy y el límite deben revisarse con el alojamiento y las pruebas de carga.

## Evidencia ejecutada

- Suite `scripts/Test-Registro.ps1`: **33 pruebas superadas, 0 fallidas, 0 omitidas**. Conserva las 20 anteriores; agrega 10 de acceso con SQL real y 3 de concurrencia del almacén.
- Los diez escenarios nuevos cubren acceso y cookie segura; denegación uniforme; permisos Cliente/Administrador y visitante; desactivación/cambio de rol/contraseña; cierre con CSRF y revocación; caducidad y renovación; datos/rol inyectado; cookie manipulada y cambio de cuenta; fallo SQL; límite de intentos.
- La caducidad se prueba con reloj controlado: acceso, actividad a los 9 minutos, actividad 9 minutos después y rechazo tras 10 minutos más sin actividad. No se sustituye el reloj por esperas reales.
- Tres pruebas deterministas intercalan renovación con limpieza o consulta y comprueban que una renovación no restaura una sesión cerrada.
- Navegador Chrome local, escritorio 1440 px y móvil 320 px: registro → acceso, error de credenciales, correo en mayúsculas, recarga, cierre, reingreso, cierre desde otra pestaña y ausencia de desbordamiento horizontal o errores JavaScript. Se utilizó una base temporal aislada; no se agregaron cuentas de prueba a la base de trabajo.
- Otra revisión de IA identificó el problema de limpieza concurrente; quedó corregido y cubierto por pruebas. No reemplaza la revisión humana de Dylan.

Capturas: [sesión en escritorio](../evidencias/acceso-escritorio.png), [credenciales inválidas](../evidencias/acceso-credenciales-invalidas.png) y [formulario móvil tras salir](../evidencias/acceso-movil-320.png).

## Siguiente responsabilidad

Preparar la inicialización controlada del primer Administrador y construir CU-09, gestión del catálogo; después CU-04, catálogo público. La cuenta Administrador usada en pruebas es ficticia y existe únicamente en la base temporal: no hay credenciales iniciales incorporadas al repositorio ni se creó un administrador operativo.

Referencias de implementación: [autenticación por cookies de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) y [antifalsificación](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0). Los requisitos proceden del DOCX y la duración fue confirmada por el líder.
