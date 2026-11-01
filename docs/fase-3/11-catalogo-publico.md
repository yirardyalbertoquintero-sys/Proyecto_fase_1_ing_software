# CU-04: consultar catálogo de productos

Avance de Yirardy en `Yirardy_Quintero`, 29 de octubre de 2026. Cubre RF-03, HU-03/HU-04 y CU-04 del DOCX vigente. Se conservó su huella; no se cambiaron esquema, migraciones ni reglas comerciales. La revisión de Dylan y la integración en main siguen pendientes.

## Recorrido disponible

Ejecutar con **F5** y pulsar **Explorar el catálogo de prendas** en registro o acceso, o **Ver catálogo** desde administración. Visitante, Cliente y Administrador pueden consultar. El inicio sigue abriendo el registro; no hace falta copiar una URL ni levantar Live Server.

La pantalla presenta tarjetas por variante con nombre, foto, talla, color, tela y precio de referencia. Permite buscar y filtrar por características sin combinar datos de Productos diferentes. El detalle se abre con **Ver prenda**, vuelve a consultar el servidor y muestra la combinación concreta. Puede cerrarse con el botón o Escape. Un enlace `catalogo.html?producto={id}` permite consultar directamente ese detalle; no concede permisos adicionales.

La disponibilidad y los precios proceden de SQL Server, no de datos de ejemplo del frontend. Si no hay activos se informa un catálogo vacío real; si falla la consulta se retiran las tarjetas anteriores y se muestra un mensaje de reintento, sin presentar el fallo como ausencia de productos. Los filtros se conservan al actualizar si sus opciones continúan disponibles.

## Contrato para Dylan y Carlos

| Ruta pública | Resultado |
| --- | --- |
| `GET /api/productos` | 200 y arreglo de variantes activas; `[]` si no hay. |
| `GET /api/productos/{id}` | 200 con características actuales; 404 si no existe o está inactivo. |

No requieren sesión ni token CSRF porque son consultas. No admiten POST, PUT o DELETE. La administración conserva su política Administrador y sus endpoints separados. Las respuestas usan `Cache-Control: no-store` para volver a comprobar la disponibilidad en cada consulta.

ProductoPublicoDto contiene únicamente id, nombre, color, talla, tela, imagenURL y precioBase. No expone la versión administrativa ni relaciones, usuarios, pedidos o estados internos. precioBase conserva su significado Decimal y viaja como cadena decimal para no perder centavos en JavaScript, igual que en CU-09. Se comprobó incluso el límite decimal(18,2).

La consulta aplica activo=true en SQL, tanto para listado como para detalle por ID. Desactivar desde CU-09 excluye la variante de las consultas siguientes y su detalle devuelve 404. El navegador vuelve a consultar al abrir/actualizar el detalle o regresar a la pestaña; las respuestas antiguas se cancelan o descartan. Esto no constituye una reserva ni evita un cambio posterior: **CU-06 debe revalidar la variante activa dentro de su propia operación antes de guardar**. Tampoco existen aún carrito, configuración ni envío de solicitudes CU-05/CU-06.

## Imágenes y presentación segura

Solo se transmiten referencias de la carpeta pública gestionada por CU-09. Si existe un registro anterior con ruta privada, externa o de disco, imagenURL se devuelve null sin alterar el dato almacenado ni exponer su ruta. No se descargan imágenes externas desde el backend. Una referencia pública cuyo archivo falta muestra **Imagen no disponible**, conservando las características y el importe del producto. Esto no declara válida una imagen ausente; su corrección corresponde a administración.

El texto de los productos se presenta mediante textContent, sin insertar HTML del catálogo. Los importes se formatean como texto decimal, sin convertirlos a Number ni calcular una cotización. El cliente identifica siempre el precio como referencial y explica que el definitivo se acuerda al cotizar.

El detalle utiliza un diálogo nativo con nombre accesible, foco, cierre y adaptación a 320 px. Las consultas de listado y detalle tienen cancelación y contador de operación. Una foto retirada de un detalle anterior no puede reemplazar la foto actual al recibir un error tardío. Las fotos se ajustan completas a sus contenedores; la ausencia o fallo de una imagen no crea un producto ficticio.

## Evidencia ejecutada

- Suite completa Release con SQL Server real: **51 pruebas aprobadas, 0 fallidas, 0 omitidas**; conserva las 44 anteriores y añade siete de catálogo público.
- Nuevos escenarios: listado vacío real; solo activos y DTO/centavos exactos; detalle con precio actualizado y posterior desactivación; consulta de Visitante/Cliente/Administrador sin permisos administrativos públicos; referencias anteriores privadas/externas no expuestas; rutas públicas sin escrituras/borrado; error SQL sin catálogo falso ni información interna.
- Chrome local: acceso desde registro sin sesión, catálogo vacío, variantes activas, filtros coherentes y conservados al actualizar, detalle, enlace directo, Escape, desactivación mientras el detalle estaba abierto, imagen ausente y fallo de consulta con reintento.
- Se comprobó que un nombre con apariencia de HTML se muestra literalmente sin ejecutarse. También se reprodujo un error tardío de la imagen A después de abrir B; la foto de B se conserva. La revisión adicional de IA identificó esa carrera y se corrigió antes de entregar; sigue pendiente la revisión humana.
- Escritorio 1440 px y móvil 320 px sin desplazamiento horizontal ni errores JavaScript. Fotos y productos ficticios creados únicamente en base temporal, retirada al terminar. Se conservó la ejecución anterior de VS Code; compilación y pruebas se realizaron en Release.

Capturas: [catálogo en escritorio](../evidencias/catalogo-publico-escritorio.png), [detalle](../evidencias/catalogo-publico-detalle.png) y [catálogo móvil](../evidencias/catalogo-publico-movil-320.png).

## Estado del trabajo de Yirardy

CU-01, CU-02, CU-09 y CU-04 cuentan con implementación local y pruebas. No equivale a que estén aceptados por el compañero, integrados en main o publicados en MonsterASP.NET. Continúan la revisión de Dylan, los contratos e integración con los CU de Dylan/Carlos, la prueba de alojamiento y las evidencias académicas conjuntas. El prototipo todavía no realiza pedidos ni pagos.
