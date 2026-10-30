# Ejecutar Impacto Textil desde VS Code

1. Abrir VS Code y seleccionar **Archivo → Abrir carpeta**.
2. Elegir la carpeta completa `C:\proyectos\Proyecto_fase_1_ing_software`. Deben verse `ImpactoTextil.slnx`, `src` y `.vscode` en el explorador.
3. Abrir **Ejecutar y depurar**, con el icono de triángulo y un insecto, o presionar **Ctrl+Shift+D**.
4. Seleccionar **Impacto Textil: ejecutar aplicación** y presionar **F5** o el triángulo verde.
5. Esperar la preparación de la base y la compilación. Cuando el servidor arranque, VS Code abrirá el formulario en el navegador automáticamente.
6. Detener la aplicación con el botón cuadrado de la barra de depuración o **Shift+F5**.

La extensión necesaria es **C# de Microsoft** (`ms-dotnettools.csharp`), incluida como recomendación del proyecto. Si VS Code estaba abierto al instalarla y solicita recargar la ventana, hacerlo para que reconozca el depurador.

El programa se ejecuta desde VS Code y su interfaz web se muestra en el navegador. No hay que copiar un enlace manualmente. **Go Live / Live Server** sirve archivos estáticos y no inicia la API C#; la ejecución configurada levanta el frontend y el backend juntos desde el mismo origen.

## Qué realiza F5

La tarea previa llama a `scripts/Iniciar-Local.ps1 -SoloPreparar -PrepararVSCode`: restaura las herramientas y dependencias, compila y aplica las migraciones a `ImpactoTextil`. Genera `.env.vscode`, que está ignorado por Git. Después el depurador ejecuta el backend y abre el navegador cuando aparece el mensaje de disponibilidad.

Si existe otra ejecución del proyecto, detenerla antes de presionar F5 para liberar el puerto y los archivos de compilación. Si esa ejecución está en PowerShell, usar Ctrl+C en su consola.

Para usar SQL Express u otra instancia, agregar `-Servidor` y el nombre de instancia a los argumentos de la tarea de preparación en `.vscode/tasks.json`; la tarea generará la configuración local correspondiente.

## Dónde editar la interfaz

- `src/frontend/index.html`: estructura de la pantalla de registro.
- `src/frontend/estilos.css`: colores, tamaños y adaptación a móvil.
- `src/frontend/registro.js`: validaciones e interacción con la API.
- `src/frontend/acceso.html` y `acceso.js`: inicio y cierre de sesión, consulta de identidad y errores de acceso. Se llega con **Inicia sesión**, desde el registro.

Los archivos del frontend se copian a la salida al compilar. Para ver un cambio, detener y volver a ejecutar con F5 y actualizar la página. Las pantallas posteriores se incorporarán cuando se implementen sus casos de uso.

## Primer Administrador y catálogo

Detén la depuración con **Shift+F5**. Con la base local preparada, usa **Terminal → Ejecutar tarea → Impacto Textil: crear primer Administrador**. La tarea solicita nombre, correo y contraseña en una terminal interactiva. No muestra ni guarda la contraseña; el comando solo crea el primero y no modifica cuentas existentes.

Después ejecuta con **F5**, inicia sesión con la cuenta Administrador y pulsa **Gestionar catálogo**. El registro público continúa creando únicamente Clientes. La pantalla está en `src/frontend/administracion.html`, con `catalogo-admin.js` y `catalogo-admin.css`.

## Ver el catálogo público

Desde el registro o la pantalla de acceso, pulsa **Explorar el catálogo de prendas**. En administración está el enlace **Ver catálogo**. La consulta es pública y muestra las variantes activas que se hayan creado, con búsqueda, filtros y detalle. Sus archivos son `src/frontend/catalogo.html`, `catalogo.js` y `catalogo.css`. No se necesitan Live Server ni una segunda aplicación.

## Preparar el paquete para alojamiento

Usa **Terminal → Ejecutar tarea → Impacto Textil: preparar publicacion**. Genera una carpeta nueva en `artifacts/entregas` después de restaurar y ejecutar todas las pruebas con SQL real. Requiere cambios de construcción guardados en Git; una carpeta que conserve NO-PUBLICAR.txt no está lista. El proceso usa Release, por lo que no necesita reemplazar la ejecución Debug abierta en VS Code.

Subir el paquete y activar HTTPS requieren la cuenta del proveedor; no son efectos de esta tarea. Ver la [guía de instalación](despliegue/INSTALAR-MONSTERASP.md).

Referencias: [configuración del depurador C#](https://code.visualstudio.com/docs/csharp/debugger-settings) y [extensión C# de Microsoft](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp).
