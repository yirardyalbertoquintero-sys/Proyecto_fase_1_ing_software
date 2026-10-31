# Actualización de la política de contraseñas

El 31 de octubre de 2026 Yirardy solicitó sustituir la política anterior de 15 caracteres sin composición obligatoria por una contraseña nueva de al menos 8 caracteres, con mayúscula, minúscula, número y símbolo. El cambio es una decisión posterior explícita del líder y queda trazado sin modificar el DOCX de fases 1 y 2 aprobado.

## Regla aplicada

RegistrarClienteDto y el formulario registro.js cuentan caracteres Unicode completos y exigen al menos una letra mayúscula, una minúscula, un dígito decimal y un signo de puntuación o símbolo Unicode. Por ejemplo, !, @, #, $, + y % cumplen el último requisito. Los espacios se permiten, pero no cuentan como símbolo. Se rechazan controles y más de 72 bytes UTF-8; la contraseña no se recorta, normaliza ni modifica antes de BCrypt.

La creación del primer Administrador reutiliza RegistrarClienteDto. Se actualizaron tanto la ayuda del formulario como el mensaje de la terminal. El algoritmo BCrypt, su coste, las cookies y los 10 minutos de inactividad de sesión se conservan. IniciarSesionDto y AccesoService continúan verificando la clave existente: las cuentas anteriores no tienen que cambiar su contraseña para entrar. No hay cambios de esquema ni migraciones.

## Comprobación

Las pruebas de validación cubren exactamente 7/8 y 72/73 caracteres ASCII con composición válida, ausencia de cada componente, espacio como falso símbolo, caracteres acentuados, emojis, controles y el límite de bytes. La prueba HTTP con SQL real rechaza combinaciones inválidas sin crear cuenta y registra/inicia sesión con una clave válida de exactamente 8 caracteres. La prueba de creación del Administrador comprueba los mismos rechazos y conserva el control contra duplicados/promoción de Clientes. Las pruebas de sesión conservan cuentas de la política anterior para verificar compatibilidad.

La suite completa terminó con **92 pruebas aprobadas, cero fallidas y cero omitidas**, incluidas 54 de integración con SQL. El resultado original está en `artifacts/politica-contrasena-20261031/politica-contrasena.trx`; el [resumen conservado](../evidencias/politica-contrasena-sql.json) identifica los resultados. Los informes de 83 pruebas del cierre administrativo anterior son históricos y no se reinterpretan como resultados de esta nueva política. Para repetir la suite con SQL Server local: `scripts/Test-Registro.ps1`.

La interfaz pasó además **20 comprobaciones** con Chrome headless: 17 combinaciones de longitud/composición/Unicode, bloqueo de seis envíos inválidos sin llamar al registro, envío válido de ocho caracteres y presentación a 320 px sin desbordamiento ni errores JavaScript. En esa comprobación visual los endpoints de registro y token se interceptaron; no se atribuye persistencia SQL a ese ensayo. El registro y acceso reales de ocho caracteres están comprobados separadamente en la suite HTTP/SQL. Ver [resultados de interfaz](../evidencias/politica-contrasena-navegador.json).

Tras guardar estos cambios, detener la ejecución anterior con Shift+F5, volver a iniciar con F5 y recargar el registro con Ctrl+Shift+R para cargar la ayuda y el JavaScript nuevos.
