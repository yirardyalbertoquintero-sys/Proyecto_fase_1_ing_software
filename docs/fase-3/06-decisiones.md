# Decisiones del diseño y asuntos pendientes

| ID | Decisión propuesta por Yirardy | Motivo y estado |
| --- | --- | --- |
| D-01 | DOCX único conservado y huella registrada. | Instrucción expresa del líder; aplicado. |
| D-02 | MonsterASP.NET Free como destino. | Coincide con el SRS; publicación pendiente de cuenta y prueba. |
| D-03 | Una aplicación ASP.NET Core con frontend del mismo origen. | Sigue la propuesta de 5.2 y 6.3; revisar con el equipo. |
| D-04 | Totales, saldo y anticipo en vistas o consultas calculadas. | Evita copias divergentes; probado en SQL. |
| D-05 | Detalle original inmutable y cantidad recuperada por referencia en cada cotización. | Preserva la fotografía y evita dependencia parcial; probado y pendiente de revisión conjunta. |
| D-06 | Constancia lógica de cotización proyectada desde su decisión; entrega persistida aparte. | Conserva los campos del SRS sin duplicar decisiones; probado y pendiente de revisión conjunta. |
| D-07 | Dos relaciones para registrar contexto de una comprobación de sublimación. | Materializa el registro de 4.6; no agrega estado ni CU. Servicio pendiente. |
| D-08 | Clave de confirmación y hash semántico para CU-06. | Prevención de duplicados solicitada por RF-05; unicidad probada, flujo HTTP pendiente. |
| D-09 | .NET 10, SDK 10.0.402 y paquetes fijados en los proyectos. | Compatible con el entorno local observado; falta validación del alojamiento. |
| D-10 | Fuente SQL de fase 3 conservada como prototipo de integridad. | EF Core es ahora la secuencia de cambios. La migración Inicial conserva 11 tablas, 30 CHECK, 5 vistas y 12 disparadores; desde ella se genera el SQL de distribución. |
| D-11 | Contraseña de al menos 15 caracteres, con espacios permitidos y sin composición obligatoria. | Confirmada expresamente por Yirardy el 28 de octubre de 2026. Límite técnico de 72 bytes UTF-8 y sin recorte. BCrypt con sal aleatoria y coste 12, probado localmente. |
| D-12 | Formulario y API del mismo origen, token antifalsificación y 12 intentos de registro por minuto por IP observada. | Controles técnicos de CU-01. El límite de intentos debe reevaluarse junto con la configuración de proxy y las pruebas RNF-05 del alojamiento. |
| D-13 | Sesión de 10 minutos de inactividad, renovable con cada petición autenticada, sin «recordarme». | Confirmada expresamente por Yirardy el 29 de octubre de 2026. Cookie de sesión Secure/HttpOnly/SameSite=Strict; caducidad comprobada con reloj controlado. Configurable mediante `Sesion:MinutosInactividad`. |
| D-14 | Sesiones revocables en memoria del servidor y cuenta/rol/credencial revalidados por petición. | Decisión técnica de CU-02 para una instancia: al salir se retira el ticket y al reiniciar el proceso se cierran todas las sesiones. No requiere nuevas tablas ni cambia el diccionario. Revisar almacenamiento compartido si el despliegue usa varias instancias. |
| D-15 | 12 intentos de acceso por minuto por IP observada; respuesta uniforme para correo ausente, clave incorrecta o cuenta inactiva. | Control técnico de CU-02, independiente del registro y sin bloqueo persistente de cuentas. Reevaluar límites y proxy en alojamiento. |

Los siguientes puntos siguen pendientes: opciones exactas de empaque, límites añadidos de texto, máximo de detalles y tamaño total de una petición del pedido, y autorización comercial de entrega con saldo cero. Las longitudes del diccionario se conservan en el diseño; las propuestas adicionales deben revisarse con el equipo. El registro fija un límite técnico de 16 KiB para su cuerpo JSON, suficiente para los campos acotados de CU-01; este límite no se aplica a la futura carga de archivos.

La contraseña ya se valida antes de aplicar BCrypt según D-11. El aviso de privacidad incluido describe únicamente la demostración con datos ficticios. El aviso definitivo y la revisión del tratamiento de datos reales siguen pendientes según el SRS.

El plan docente exige 80 % de los CU principales; el objetivo sigue siendo construir los seis principales y sus apoyos hasta completar los doce previstos. La referencia académica se registra al consolidar las fases 3 y 4. La fecha de defensa sigue pendiente de confirmación.
