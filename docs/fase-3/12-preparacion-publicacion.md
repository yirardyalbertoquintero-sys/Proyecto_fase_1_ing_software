# Preparación de la publicación

Trabajo de coordinación de Yirardy, 29 de octubre de 2026. No añade un caso de uso comercial ni cambia el stack. Los cuatro CU disponibles siguen siendo CU-01, CU-02, CU-04 y CU-09. El documento aprobado permanece intacto.

## Resultado disponible

`scripts/Preparar-Publicacion.ps1` automatiza la restauración fijada, pruebas completas con SQL Server real, publicación Release, generación del SQL idempotente desde las migraciones, manifiesto e inventario SHA-256. El manifiesto identifica commit, rama, SDK, huella de la fuente y contadores de pruebas. La composición de la solución y los archivos que construyen o documentan el paquete forman parte de la comprobación de cambios locales.

Una preparación normal requiere guardar primero los cambios de construcción en Git. Un ensayo con `-PermitirCambiosLocales` se etiqueta como tal y no atribuye todos los bytes al commit. Si cambia el commit durante la preparación o aparece una modificación local sin ese modo, se rechaza el cierre. Una carpeta nueva conserva NO-PUBLICAR.txt hasta terminar; el inventario no incluye ese indicador temporal. No se sobrescriben entregas anteriores.

Las pruebas deben tener total positivo, todas aprobadas y cero fallidas/omitidas. La fábrica de diseño de EF requiere una base propia en la cadena de conexión; el script proporciona un nombre ficticio solo para generar SQL, sin ejecutar ese SQL. Las variables de conexión se restauran en finally.

El ZIP contiene solo la aplicación. SQL, resultados de pruebas, guía, manifiesto e inventario permanecen fuera del sitio web. Las configuraciones locales por entorno, certificados, .env, perfiles y storage están excluidos de la publicación y se comprueba el resultado antes de comprimir. Así la actualización no transporta secretos ni reemplaza los archivos persistentes del sitio. También se ampliaron las exclusiones de Git para esas configuraciones y perfiles.

## Validación ejecutada

Se ensayó la preparación desde Windows PowerShell 5.1. El paquete de ensayo indica expresamente cambios locales porque se construyó mientras se desarrollaban los scripts.

| Comprobación | Resultado observado | Límite |
| --- | --- | --- |
| Suite completa con SQL real | 51 aprobadas, 0 fallidas, 0 omitidas. | Reglas y recorridos ya implementados; no los CU pendientes. |
| Exclusión de archivos locales | Configuración, perfil y archivo storage ficticios no aparecen en el sitio generado. | No detecta secretos escritos deliberadamente dentro de código o contenido público. |
| ZIP e inventario | 72 entradas del sitio y 77 huellas verificadas en el ensayo, sin SQL/storage/perfiles/configuración local en el ZIP. | Los recuentos pueden variar en futuras versiones. |
| SQL generado | Aplicado dos veces en una base temporal, sin recrear el esquema. | No es una ejecución en SQL Server de MonsterASP.NET. |
| Aplicación publicada | DLL ejecutada desde la carpeta app, en Production y con configuración externa. | Kestrel local; no acredita el módulo IIS del proveedor. |
| Comprobación de lectura | 14 aprobadas con certificado HTTPS local validado por el sistema, sin omitir TLS. | No se conectó al subdominio público del proyecto. |
| Escritura y reinicio | Administrador ficticio, acceso, foto y variante creados en base temporal. Tras reiniciar, producto y foto mantienen sus datos y bytes. La cookie anterior recibe 401, conforme al almacén de sesiones en memoria. | Cuenta y datos ficticios; no se creó Administrador operativo. |
| Rutas directas | Configuración, SQL, .git y storage no se sirven directamente; administración denegada al visitante. | No sustituye las futuras descargas privadas autorizadas de CU-11. |

Las bases y carpetas temporales de comprobación se retiran al terminar. La ejecución anterior de VS Code y la base de trabajo del usuario se conservan.

## Comprobador de lectura

`Test-Publicacion.ps1` comprueba la raíz HTTPS, catálogo HTML/JS, disponibilidad SQL, consulta del catálogo, 401 de sesión/administración, seis rutas no públicas y redirección HTTP al mismo host/puerto HTTPS. No sigue redirecciones automáticamente ni acepta certificados inválidos. Las respuestas cuyo cuerpo se comprueba están limitadas a 1 MiB y 20 segundos, incluidos sus bytes, para evitar esperas indefinidas. El informe no conserva cuerpos, tokens, cookies ni conexiones.

Revisión adicional de IA: se revisaron normalización de rutas Windows, composición de construcción y timeout de lectura. La implementación usa DirectorySeparatorChar, incluye la solución en la trazabilidad y utiliza ResponseContentRead con límite de tamaño para los cuerpos. No sustituye la revisión humana del compañero.

## Pendiente en el proveedor

Yirardy confirmó que **todavía no tiene cuenta MonsterASP.NET**. Por ello no se atribuyen publicación pública, runtime efectivo, permisos de disco, conexión remota, HTTPS del subdominio ni cuotas aprobadas. La [guía operativa](../despliegue/INSTALAR-MONSTERASP.md) explica la instalación y el primer Administrador sin compartir contraseñas.

Después de crear cuenta, sitio y base MSSQL: confirmar .NET 10 y almacenamiento persistente, configurar secretos, revisar/aplicar el SQL en la base correcta, subir solo el programa y activar HTTPS. En Free, la guía oficial indica publicar primero y solicitar HTTPS a soporte. Ejecutar el comprobador contra la dirección real y repetir escrituras/reinicio. No desactivar HTTPS o cookies Secure para resolver la ausencia de certificado.

Se mantienen pendientes la revisión de Dylan, integración con los CU de Dylan/Carlos, prueba de archivos privados autorizados y RNF de rendimiento, concurrencia y disponibilidad mensual. El alcance funcional no se amplió ni se marcan esas condiciones como cumplidas.
