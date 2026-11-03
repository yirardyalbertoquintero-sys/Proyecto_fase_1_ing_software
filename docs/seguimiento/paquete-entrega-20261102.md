# Paquete de entrega de los doce CU

Preparado el 2 de noviembre de 2026 (hora de Managua), desde el commit 12b4e3a377f9ea8fa13e821e8978e0817c680ab8 de Yirardy_Quintero, sin cambios de construcción locales. El identificador usa UTC, por eso empieza por 20261103. Responsable del despliegue: **Yirardy**, por su indicación más reciente. Alojamiento todavía sin validar.

## Contenido generado

Carpeta local ignorada por Git: `artifacts/entregas/20261103T025640Z-12b4e3a-17ecc259`.

- `sitio.zip` (29 268 824 bytes): aplicación compilada; `app/` conserva el mismo contenido descomprimido.
- `database/actualizar.sql`: SQL idempotente de Inicial y HuellaComprobanteCliente. Generarlo no lo ejecuta en la base del negocio ni copia datos locales.
- `LEEME-INSTALAR.md`: guía actualizada de variables, rutas persistentes públicas/privadas, HTTPS, primer Administrador, comprobaciones y respaldo/restauración.
- `manifiesto.json`: doce CU, versión, commit, pruebas y responsable; alojamientoValidado=false.
- `SHA256.csv` y `pruebas/resultado.trx`: inventario y evidencia de la ejecución.

## Resultado real

La tarea Preparar-Publicacion.ps1 ejecutó 124 pruebas aprobadas, cero fallidas y cero omitidas, en Release. Restauró dependencias fijadas, publicó y generó SQL; finalizó correctamente y retiró NO-PUBLICAR.txt.

Después se comprobaron los 100 archivos del inventario, sus tamaños y huellas. Los 95 archivos del ZIP coinciden con app/. Los recursos de frontend publicados coinciden con src/frontend, cuya versión sigue siendo la interfaz anterior al rediseño retirado. No se modificó código de aplicación, reglas, interfaz ni migraciones al preparar este paquete. Los documentos aprobados y el Word personal sin seguimiento se conservaron.

Se arrancó la DLL del paquete en Production local con conexión SQL local y se realizaron doce consultas de lectura: pantallas, JavaScript, disponibilidad y catálogo; sesión/pedidos protegidos devuelven401 y configuración/SQL no públicos devuelven404. Se detuvo esa instancia temporal. Se usó un certificado de desarrollo aceptado solo para esa comprobación; no demuestra HTTPS público ni compatibilidad con el proveedor. No se iniciaron sesiones ni se enviaron pedidos o pagos en este recorrido de arranque.

Resumen reproducible y huellas en `docs/evidencias/paquete-entrega-20261102.json`. El registro local detallado de verificación está en `artifacts/verificacion-paquete-20261102`.

## Siguiente paso

Yirardy conservará el paquete en su computadora y realizará la instalación en su cuenta del proveedor siguiendo LEEME-INSTALAR.md. Publicar solamente sitio.zip o el contenido de app/; SQL, manifiesto, pruebas y guías quedan fuera del sitio web. Configurar credenciales externamente y carpetas persistentes separadas. El despliegue real, la comprobación de escritura/archivos/reinicio en el proveedor y el ensayo de restauración quedan pendientes. Las comprobaciones en destino complementan la revisión local ya terminada.
