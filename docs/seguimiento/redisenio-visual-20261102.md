# Renovación visual de Impacto Textil

2 de noviembre de 2026. Solicitada por Yirardy: mejorar la presentación de cliente y administración sin modificar funciones y conservando una aplicación ligera.

## Cambios

- Paleta compartida, tipografía del sistema y controles coherentes. Navegación y formularios más compactos, tarjetas de importes y estados con mayor jerarquía visual.
- Registro y acceso con ilustración SVG local; catálogo con portada textil y tarjetas renovadas; configuración y carrito en paralelo en escritorio, apilados en móvil.
- Administración de catálogo y expedientes con menor tamaño de encabezados, separación de formularios, historial y descargas. Códigos largos se ajustan sin ocultarlos.
- Se conservan todos los controles, IDs, nombres, atributos de validación, mensajes, enlaces y scripts. Solo se sustituye arte decorativo y se añaden envoltorios/clases de presentación; el backend, JavaScript, base de datos y reglas no cambian.
- Sin frameworks, fuentes remotas, imágenes de gran tamaño ni animaciones continuas. El SVG añadido pesa menos de 2 KB. Los avisos académicos y textos de precios, consentimiento y pagos permanecen.

## Comprobaciones

40 comprobaciones de interfaz en Edge headless con datos/API simulados, incluyendo ocho pantallas a 1440, 768, 390 y 320 px, ausencia de desbordamiento horizontal, detalle/cierre con Escape, búsqueda, errores de registro, selección de edición e historial. Sin errores JavaScript. Se revisaron capturas de escritorio y móvil.

25 comprobaciones separadas de cambio de pestaña: posición, carrito, archivos File, borradores, historial desplegado, permisos, errores y respuestas tardías. Los escenarios usan respuestas simuladas y eventos controlados; no acreditan SQL ni una revisión humana del nuevo aspecto. Se conservó la reproducción del defecto anterior como control.

Compilación Release sin errores ni advertencias. Comparación estática de elementos y atributos funcionales de los HTML frente a la base; no se modificaron reglas ni componentes de servidor. La suite SQL de 124 pruebas del cierre anterior sigue como evidencia de ese corte y no se cuenta como una ejecución nueva de este cambio visual.

Capturas, resultados detallados y datos ficticios locales en artifacts/redisenio-visual-20261102. Resumen y huellas en docs/evidencias/redisenio-visual-20261102.json. No se incluyen cuentas ni capturas de clientes reales.

## Cómo verlo

En la carpeta habitual del proyecto, rama Yirardy_Quintero: detener la depuración anterior con Shift+F5 y ejecutar F5. Al abrir el navegador, Ctrl+F5 fuerza la actualización de los estilos guardados. Revisar catálogo, acceso, solicitud, Mis pedidos y administración. El cambio se prepara para revisión por PR antes de integrarlo a main; no implica publicación en el proveedor.
