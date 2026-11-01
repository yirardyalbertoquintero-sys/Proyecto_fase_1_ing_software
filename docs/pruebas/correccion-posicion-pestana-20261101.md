# Corrección de posición al regresar a la aplicación

Fecha: 1 de noviembre de 2026. Responsable: Yirardy, con asistencia de IA. Cambio preparado en Yirardy_Quintero a partir de main integrado; no modifica el documento aprobado, reglas del negocio, API ni esquema SQL.

## Defecto y resultado

Al pasar de Edge a otra ventana y regresar, la revalidación de acceso ocultaba temporalmente secciones completas mediante hidden o vaciaba listados. Al reducirse la altura, el navegador limitaba el desplazamiento y llevaba la página hacia arriba. Algunos enfoques automáticos también desplazaban la pantalla. Se reprodujo con el código anterior: pedidos pasó de 900 a 0 píxeles.

posicion-pestana.js conserva temporalmente la altura y la posición mientras se comprueba el acceso. Al terminar, retira esa altura y restaura la posición para la misma identidad autorizada. Una acción de desplazamiento del usuario durante la consulta tiene prioridad. En Mis pedidos conserva también las secciones de historial desplegadas. Los enfoques de revalidación usan preventScroll; los formularios siguen manteniendo sus borradores en memoria.

Se aplica a Mi sesión, catálogo público, catálogo administrativo, Solicitar cotización, Mis pedidos y administración de pedidos. Las validaciones de sesión y permisos continúan activas. Los errores o la pérdida de acceso retiran la altura temporal y mantienen ocultos los expedientes que no corresponde mostrar. Las revalidaciones concurrentes y las consultas anteriores a login/logout no sustituyen el estado vigente.

## Comprobación ejecutada

- Compilación Debug correcta, sin errores ni advertencias; comprobación de sintaxis de los siete JavaScript implicados.
- **25 comprobaciones de navegador aprobadas**, incluida la reproducción del defecto anterior. [Resultados](../evidencias/correccion-posicion-pestana-20261101.json).
- Edge en modo headless, con el HTML/CSS/JavaScript real y una API ficticia local. Eventos visibilitychange y pageshow controlados para hacer repetibles las esperas y respuestas fuera de orden. No acredita un cambio de aplicación manual en el equipo ni pruebas SQL adicionales.
- Solicitud en escritorio y a 320 píxeles: posición, carrito, texto y File conservados. Mis pedidos: historial abierto, monto, referencia y archivo en memoria. Catálogos: posición y edición conservadas; detalle modal abierto al regresar.
- Comprobados sesión vencida, cambio de cuenta, fallo de consulta, dos revalidaciones simultáneas, interacción durante la espera, login/logout y guardado en curso. Una consulta antigua liberada después del cierre no vuelve a mostrar la cuenta cerrada.
- Cambiar de ventana no generó escrituras. Las escrituras de los casos de login/logout y guardado se hicieron únicamente contra la API ficticia por acciones explícitas de la prueba. No se usó ni alteró la base del usuario.
- Revisión estática de otro asistente completada, con correcciones de los hallazgos de concurrencia y sin bloqueantes restantes en el cambio. No sustituye la revisión humana del equipo.

## Verificación manual pendiente

1. Con la aplicación local iniciada, actualizar Edge con Ctrl+F5 para cargar los archivos nuevos.
2. Abrir una de las pantallas indicadas, bajar y escribir un dato sin enviarlo; en Solicitud o Mis pedidos, seleccionar también un archivo ficticio.
3. Pasar a ChatGPT u otra ventana y regresar a Edge varias veces. Comprobar posición, valores, archivo conservado e historial abierto.
4. En una prueba aparte, cerrar la sesión desde otra pestaña y regresar. Debe solicitar acceso o denegarlo; conservar la posición no autoriza a mostrar datos privados.

La evidencia anterior de 124 pruebas corresponde al cierre integrado previo; estas 25 comprobaciones de interfaz se registran por separado. No se declara realizada esta revisión manual ni el despliegue público.

Referencias técnicas: [foco sin desplazamiento](https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/focus) y [visibilidad de la página](https://developer.mozilla.org/en-US/docs/Web/API/Page_Visibility_API).
