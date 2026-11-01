# CU-09: administración del catálogo y primer Administrador

Avance de Yirardy en su rama `Yirardy_Quintero`, 29 de octubre de 2026. Implementa RF-14/HU-14/CU-09 y la inicialización prevista en 6.2 del DOCX vigente. No modifica ese documento, las once tablas, las migraciones ni los importes históricos. Revisión humana de Dylan e integración en main pendientes.

## Cómo probarlo desde VS Code

1. Detener la ejecución anterior con **Shift+F5**. La base local debe estar preparada con la tarea normal de F5 o `scripts/Iniciar-Local.ps1 -SoloPreparar`.
2. Ejecutar **Terminal → Ejecutar tarea → Impacto Textil: crear primer Administrador**. Alternativa en la terminal del proyecto: `./scripts/Crear-Administrador.ps1`; admite `-Servidor '.\SQLEXPRESS'` si corresponde.
3. Escribir nombre, correo y una contraseña propia de al menos 8 caracteres, con mayúscula, minúscula, número y símbolo. Admite espacios (no cuentan como símbolo) y hasta 72 bytes UTF-8. La contraseña se solicita dos veces, no se muestra y no se pasa por argumentos ni se guarda en archivos. Política actualizada por Yirardy el 31 de octubre de 2026.
4. Ejecutar con **F5**, pulsar **Inicia sesión** e ingresar con esa cuenta. Aparece el enlace **Gestionar catálogo**.
5. Crear una variante con foto, revisar el listado y editar datos o disponibilidad. Cada combinación de talla, color y tela es un Producto independiente. Los precios se rotulan como referencia, no como una cotización aceptada.

No existe una cuenta predeterminada. La aplicación no agrega productos ni fotos ficticios a la base del usuario. La cuenta usada en la comprobación de terminal existió solo en una base temporal, retirada al terminar.

## Inicialización controlada

`--crear-administrador` ejecuta un comando local y sale sin abrir el servidor HTTP. Requiere entrada interactiva y acceso a la base configurada. No existe endpoint público para asignar ese rol.

El servicio reutiliza la validación confirmada y BCrypt, obtiene un bloqueo de aplicación asociado a una transacción SQL, comprueba que no exista ningún Administrador y que el correo no pertenezca a otra cuenta, y crea el primero. Dos ejecuciones simultáneas solo crean uno. Un Administrador inactivo también impide repetir la inicialización: este comando no es recuperación de acceso, no promueve Clientes ni restablece contraseñas.

La cuenta se crea con rol Administrador y activa. No se atribuye un consentimiento no recibido: aceptacionTerminos=false y fechaAceptacion=null, combinación permitida para el alta administrativa por el esquema. El registro público conserva sus exigencias y asigna únicamente Cliente.

## Contratos de CU-09

| Operación | Resultado / control |
| --- | --- |
| `GET /api/administracion/productos` | Lista variantes activas e inactivas; solo Administrador. |
| `GET /api/administracion/productos/{id}` | Consulta una variante; 404 si no existe. |
| `POST /api/administracion/productos` | Crea con nombre, color, talla, tela, imagenURL, precioBase y activo. Devuelve 201 y ProductoDto. |
| `PUT /api/administracion/productos/{id}` | Guarda todos los campos anteriores junto con version. Devuelve 200; 400 si falta versión/datos, 404 si no existe y 409 si otra edición cambió la variante. |
| `POST /api/imagenes-catalogo` | Multipart con campo archivo; JPG/PNG validado. Devuelve imagenURL generado. Solo Administrador, CSRF y límite técnico de 12 subidas/minuto por IP observada. |
| `GET /api/imagenes-catalogo/{archivo}` | Foto pública existente y referenciada por algún Producto. No sirve archivos privados ni fotos subidas todavía sin asociar. |

Todas las escrituras exigen CSRF. Una sesión ausente recibe 401 y un Cliente recibe 403, sin ejecutar la modificación. No hay operación DELETE. Desactivar se realiza editando activo=false; el producto y su referencia permanecen.

Los límites textuales coinciden con el diccionario: nombre 100, color 50, talla 30, tela 100 e imagenURL 2048 caracteres. Se rechazan campos ajenos, campos obligatorios vacíos, controles de texto y precios no positivos o con más de dos decimales. El máximo técnico es decimal(18,2), sin introducir una tarifa comercial máxima.

precioBase conserva el tipo lógico Decimal y se transporta en JSON como cadena decimal para no perder centavos al pasar por JavaScript; también se acepta un número JSON válido. Ejemplo: `"precioBase": "150.25"`. La interfaz no convierte el importe a Number para enviarlo o formatearlo. El contrato no cambia los nombres del diccionario.

## Concurrencia e historial

version es una huella técnica SHA-256 del contenido de la variante, no una nueva tabla ni un atributo comercial del SRS. El precio se representa siempre con dos decimales al calcularla: 150 y 150.00 generan la misma huella. En edición, el repositorio obtiene UPDLOCK/HOLDLOCK sobre el Producto dentro de una transacción, vuelve a calcular la huella y solo guarda si coincide con la recibida. Así, dos ediciones sobre la misma versión no se sobrescriben silenciosamente. Un conflicto conserva los campos del formulario para que el usuario recargue y revise.

El repositorio actualiza solo Producto. No reescribe DetallePedido, precioBaseReferencia, DetalleCotizacion ni precios acordados. Se probó que cambiar precio, talla, color y disponibilidad conserva la configuración original y una cotización aceptada existente. La baja queda persistida; impedir nuevas selecciones por pantalla y al registrar solicitudes debe integrarse y verificarse con CU-04/CU-05/CU-06, que siguen pendientes. No se presenta esa integración como terminada.

## Fotos públicas y almacenamiento

El Administrador selecciona un archivo desde el dispositivo; no se aceptan URL externas, rutas arbitrarias o referencias a diseños/comprobantes privados. Se valida extensión y firma, se leen bytes con límite y se comprueban dimensiones y decodificación real mediante **StbImageSharp 2.30.16**. Se admiten JPG/JPEG y PNG. No se confía en Content-Type ni en el nombre original.

Los límites técnicos de catálogo son 10 000 000 bytes inclusivos y 12 millones de píxeles; el cuerpo multipart permite 10 100 000 bytes para incluir su envoltura. La decodificación se serializa para acotar uso simultáneo de memoria. Estos límites de fotos públicas no sustituyen las reglas de originales privados ni permiten reducir o transformar diseños del cliente. Las cuotas y rendimiento del alojamiento aún deben verificarse.

Los nombres se generan en servidor; se conserva el JPG/PNG validado en `storage/catalogo` del backend, fuera de wwwroot y de Git. `Catalogo:CarpetaImagenes` permite configurar una carpeta persistente diferente. El endpoint devuelve un tipo image/jpeg o image/png fijo con nosniff y solo sirve archivos que utiliza un Producto. La vista previa local usa blob; la CSP permite blob únicamente en img-src.

Subir una foto y guardar la variante son dos operaciones. Cancelar o fallar después de la subida puede dejar un archivo sin referencia: no queda accesible por el endpoint público ni se elimina automáticamente ante un resultado incierto. Tampoco se borran fotos anteriores al editar. La conciliación y retirada de archivos sin referencia se hará mediante mantenimiento controlado; no hay un limpiador automático en este avance. Los respaldos deben incluir base y carpeta de fotos coherentes, y el despliegue debe conservar esa carpeta al actualizar el programa.

## Validación ejecutada

- **44 pruebas aprobadas, 0 fallidas, 0 omitidas**, con SQL Server real y configuración Release. Se conservan las 33 anteriores y se agregan diez de catálogo y una de inicialización. Las pruebas rápidas y SQL se incluyen en el mismo total.
- Alta, consulta, edición y baja sin borrado; permisos de Visitante/Cliente; rechazo de referencias privadas/externas; fotos reales JPG y PNG, falsas, dañadas, formato incoherente y límite inclusivo de bytes; datos/CSRF; conflicto simultáneo; precio entero y versión estable al volver de SQL; preservación de detalles y cotización aceptada.
- Inicialización: contraseña inválida, correo de Cliente sin promoción, dos creaciones simultáneas, una sola cuenta Administrador, hash verificable y ausencia de consentimiento ficticio. Se probó también el comando interactivo real en una base temporal: no mostró la contraseña.
- Chrome local en escritorio 1440 px y móvil 320 px: visitante denegado, acceso de Administrador, creación con foto y vista previa, edición de precio entero, variante inactiva, filtros/búsqueda y mensaje de conflicto sin sobrescritura. Sin errores JavaScript ni desplazamiento horizontal.
- Revisión adicional de IA: detectó la diferencia de escala decimal en la huella; se corrigió con representación canónica y se añadió su prueba. No sustituye la revisión de Dylan.

Capturas: [escritorio](../evidencias/catalogo-admin-escritorio.png) y [móvil 320 px](../evidencias/catalogo-admin-movil-320.png). Los datos y la imagen de esas capturas son ficticios y solo existieron en la base temporal de navegador. La compilación Debug anterior permaneció abierta en VS Code durante las comprobaciones; las pruebas se ejecutaron en Release para no detenerla.

El avance posterior de **CU-04** ya está implementado y documentado en [Catálogo público](11-catalogo-publico.md). Continúa la coordinación con la configuración/solicitud de Dylan, la revisión humana y la comprobación del alojamiento. No se acredita todavía el envío de pedidos.

Referencias técnicas: [concurrencia en EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [StbImageSharp y su licencia dual Public Domain/MIT](https://github.com/StbSharp/StbImageSharp) y [paquete fijado](https://www.nuget.org/packages/StbImageSharp/2.30.16). Se eligió su distribución bajo Unlicense; no añade un framework de frontend ni cambia el stack del proyecto.
