# Resumen integral de cambios realizados a la plataforma — 30 de octubre de 2026

## 1. Propósito y estado real de la plataforma

Este documento consolida todos los cambios técnicos, funcionales, de base de datos, seguridad, empaquetado y documentación incorporados hasta la fecha en el repositorio de **Impacto Textil** (rama `Yirardy_Quintero`), contrastados contra la única fuente válida de requisitos: [Impacto textil fase 1 y 2.docx](../Impacto%20textil%20fase%201%20y%202.docx) (`SHA-256: 9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`).

Para mantener trazabilidad estricta y no presentar trabajo en curso como casos de uso terminados, los cambios actuales se dividen en dos niveles claros:

1. **Módulos implementados y probados localmente de extremo a extremo (pantalla, API, servicios, persistencia y pruebas):**
   - Comprobación técnica de disponibilidad (`GET /api/disponibilidad`).
   - **CU-01 (`RF-01`):** Registro público de nueva cuenta de cliente con BCrypt y consentimiento fechado.
   - **CU-02 (`RF-02`):** Inicio y cierre de sesión, revocación en servidor, expiración por inactividad de 10 minutos y políticas de rol (`Cliente` y `Administrador`).
   - **CU-09 (`RF-14`) + Inicialización controlada:** Creación interactiva por consola del primer Administrador, alta/consulta/edición de variantes del catálogo con control de concurrencia, subida validada de fotos públicas y baja lógica sin borrado físico.
   - **CU-04 (`RF-03`):** Catálogo público interactivo y responsivo con búsqueda, filtros por variante y consulta en vivo del detalle de prendas activas.
   - **Automatización de entrega reproducible:** Empaquetado Release, generación idempotente de SQL, manifiesto SHA-256 y comprobador de lectura HTTPS.
2. **Base preparatoria de backend para los módulos administrativos de pedidos, diseños, pagos y producción (`CU-10`, `CU-11`, `CU-12` y `CU-13`):**
   - Contratos DTO de entrada/salida, motor de reglas puras del dominio administrativo, validador/almacén local de archivos privados fuera de `wwwroot`, repositorio transaccional con bloqueo pesimista sobre `Pedido` y servicios del modelo (`RevisionCotizacionAdminService`, `DescargaDisenoAdminService`, `PagosAdminService` y `ProduccionEntregaAdminService`).
   - **Alcance actual de este bloque:** Compila limpiamente en `.NET 10.0.402` sin advertencias ni errores y sin alterar `Program.cs` ni las migraciones compartidas, pero **aún no expone endpoints HTTP ni pantallas administrativas**, ni cuenta todavía con su suite propia de pruebas ejecutadas. Por tanto, `CU-10`, `CU-11`, `CU-12` y `CU-13` permanecen **en construcción**, a la espera de conectar controladores, vistas e integrarse con `CU-05`–`CU-08`.

---

## 2. Gobernanza documental, entorno y organización del repositorio

- **Preservación de la fuente vigente y archivo histórico:**
  - Copia intacta del SRS aprobado en `docs/Impacto textil fase 1 y 2.docx` y registro de procedencia en [FUENTE_VIGENTE.md](../FUENTE_VIGENTE.md).
  - Respaldo e inventario verificados por SHA-256 de 24 documentos antecedentes en `docs/archivo/INVENTARIO.csv`, separados de la documentación activa mediante [INDICE.md](../INDICE.md).
  - Incorporación de guías docentes en `docs/guias/` y plan de trabajo del equipo en `docs/planes/Plan_Fases_3_y_4_Impacto_Textil.md`.
- **Estandarización del entorno de desarrollo:**
  - Fijación de SDK `.NET 10.0.402` en `global.json`, bloqueo determinista de dependencias transitivas con `packages.lock.json` y conversión de vulnerabilidades NuGet (`NU1902`–`NU1904`) en errores de compilación dentro de `Directory.Build.props`.
  - Configuración lista para VS Code en `.vscode/launch.json` y `.vscode/tasks.json`, documentada en [ejecutar-en-vscode.md](../ejecutar-en-vscode.md).
- **Documentación técnica de Fase 3 (`docs/fase-3/`):**
  - Modelo relacional (`01-modelo-relacional.md`), normalización hasta 3FN (`02-normalizacion.md`), arquitectura (`03-arquitectura.md`), despliegue (`04-despliegue.md`), matriz de trazabilidad (`05-trazabilidad.md`), decisiones (`06-decisiones.md`), flujo de ramas (`07-flujo-equipo.md`), evidencias de `CU-01` a `CU-04`/`CU-09` (`08` a `11`) y preparación de publicación (`12-preparacion-publicacion.md`).
  - Diagramas editables en `docs/fase-3/diagramas/Impacto_Textil_Fase_3.drawio`, fuentes Mermaid (`.mmd`) y exportaciones vectoriales (`.svg`) generadas con `scripts/Generar-Diagramas.py`.

---

## 3. Modelo de datos, vistas y migraciones en SQL Server

El esquema se consolidó en Entity Framework Core (`ImpactoTextilDbContext`, `ImpactoTextilDbContext.Restricciones.cs`, migración `20261028165048_Inicial` e `InicialComplementos.cs`), generando el script idempotente `database/002_migracion_inicial.sql`:

- **11 tablas del dominio e historial:**
  - Núcleo comercial: `Usuario`, `Producto`, `Pedido`, `DetallePedido`, `Pago`.
  - Historial y constancias inmutables: `VersionDiseno`, `VersionCotizacion`, `DetalleCotizacion`, `ConstanciaEntrega`, `ComprobacionSublimacion` y `DisenoComprobado`.
- **Integridad declarativa y procedimental en el motor:**
  - **30 restricciones `CHECK`** para rangos monetarios positivos, medios de pago válidos, estados permitidos, metadatos obligatorios en decisiones/archivos, formato de hash BCrypt y consentimiento.
  - **Índices únicos** para correo sin distinción de mayúsculas (`Latin1_General_100_CI_AS_SC`), código de pedido, confirmaciones idempotentes (`UQ_Pedido_confirmacion`, `UQ_Pago_confirmacion`), numeración correlativa de versiones y unicidad filtrada de referencias de pagos verificados (`UX_Pago_referenciaVerificada`).
  - **5 vistas calculadas** (`vPedido`, `vDetallePedido`, `vVersionCotizacion`, `vDetalleCotizacion` y `Constancia`) que proyectan totales referenciales, totales aceptados, anticipo fijo del 50 % con redondeo hacia arriba al centavo (`CEILING(total * 50) / 100`), saldos pendientes y constancias sin duplicar columnas calculadas.
  - **12 disparadores (`TRIGGER`)** que impiden sobrescribir o borrar versiones resueltas, pagos ya decididos, detalles originales de solicitud, constancias y comprobaciones históricas de sublimación. EF Core desactiva la cláusula `OUTPUT` directa (`UseSqlOutputClause(false)`) para compatibilidad plena con los disparadores de SQL Server.

---

## 4. Seguridad transversal e infraestructura común de la API

Implementada en `Program.cs` y `src/backend/ImpactoTextil.Api/Infrastructure/`:

- **Mismo origen y cabeceras HTTP:** El frontend estático se copia a `wwwroot` y se sirve desde el mismo proceso ASP.NET Core con redirección HTTPS, HSTS fuera de desarrollo, `X-Content-Type-Options: nosniff`, `Referrer-Policy: same-origin` y `Content-Security-Policy` restrictiva (`default-src 'self'`, `img-src 'self' blob:`, `frame-ancestors 'none'`).
- **Protección CSRF:** Endpoint `GET /api/seguridad/token` (`SeguridadController`) y filtro global `FiltroAntifalsificacion` que exige `X-CSRF-TOKEN` sincronizado con la cookie `__Host-ImpactoTextil.Antiforgery` (`Secure`, `HttpOnly`, `SameSite=Strict`) en toda petición que modifique estado.
- **Validación estricta de contratos y errores seguros:** Todos los DTO de entrada aplican `[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]` para rechazar propiedades inyectadas (como `rol`). `ManejadorErrores` intercepta fallos no controlados devolviendo `ProblemDetails` con código de referencia sin exponer cadenas de conexión, consultas ni pilas de llamadas.
- **Control de abuso (`RateLimiter`):** Ventanas fijas de 12 intentos por minuto por IP para `registro`, `acceso` e `imagenes-catalogo`, respondiendo `429 Too Many Requests` con `Retry-After: 60`.
- **Disponibilidad técnica:** `GET /api/disponibilidad` (`DisponibilidadController` y `ConsultaDisponibilidadSql`) verifica conectividad real con SQL Server devolviendo `200 OK` o `503 Service Unavailable` sin filtrar datos internos.

---

## 5. Casos de uso implementados de extremo a extremo

### 5.1 CU-01 — Registrar nueva cuenta (`RF-01`, `RNF-03`, `RNF-04`)
- **Frontend:** `src/frontend/index.html`, `aviso.html`, `estilos.css` y `registro.js` (adaptable desde 320 px, validación previa en cliente, prevención de doble envío y aviso explícito de demostración académica con datos ficticios).
- **Backend:** `ClientesController` (`POST /api/clientes`), `RegistrarClienteDto`, `RegistroClienteService`, `UsuarioRepository` y `ProtectorContrasena`.
- **Reglas aplicadas:** Contraseña de al menos 15 caracteres Unicode (espacios permitidos) y máximo 72 bytes UTF-8 sin truncamiento silencioso; hash BCrypt con coste 12; rol fijo `Cliente`; registro obligatorio de fecha de aceptación de términos y manejo concurrente de correos duplicados (`409 Conflict`).

### 5.2 CU-02 — Iniciar y cerrar sesión (`RF-02`, `RNF-03`, `RNF-04`)
- **Frontend:** `src/frontend/acceso.html` y `acceso.js` (consulta de sesión activa, alternancia para mostrar/ocultar clave, visualización de identidad/rol y cierre seguro).
- **Backend:** `SesionController` (`POST /api/sesion`, `GET /api/sesion`, `DELETE /api/sesion`), `IniciarSesionDto`, `AccesoService`, `AlmacenSesiones` y `ValidarSesion`.
- **Reglas aplicadas:** Cookie `__Host-ImpactoTextil.Sesion` no persistente (`Secure`, `HttpOnly`, `SameSite=Strict`), inactividad renovable de **10 minutos** (`Sesion:MinutosInactividad`), verificación BCrypt constante ante correos inexistentes para uniformar la respuesta `401`, revocación inmediata en servidor al cerrar sesión e invalidación automática si el usuario pasa a inactivo o cambia de rol/hash. Políticas de autorización `Cliente` y `Administrador` activas.

### 5.3 Inicialización del primer Administrador y CU-09 — Gestionar catálogo (`RF-14`)
- **Alta controlada del primer Administrador:** Modo `--crear-administrador` (`InicializarAdministrador`, `AdministradorInicialService`) y script interactivo `scripts/Crear-Administrador.ps1` con bloqueo exclusivo en SQL Server (`sp_getapplock`). Rechaza la operación si ya existe cualquier Administrador o si el correo pertenece a un cliente, sin guardar credenciales en archivos ni argumentos.
- **Frontend administrativo:** `src/frontend/administracion.html`, `catalogo-admin.css` y `catalogo-admin.js`.
- **Backend administrativo:** `CatalogoAdministracionController` (`GET/POST/PUT /api/administracion/productos`), `ImagenCatalogoController` (`POST/GET /api/imagenes-catalogo`), `GuardarProductoDto`, `ProductoVersion`, `CatalogoService`, `ProductoRepository` e `ImagenCatalogoStore`.
- **Reglas aplicadas:** Transporte de `precioBase` como cadena decimal exacta; concurrencia optimista/pesimista combinando huella SHA-256 canónica (`version`) y bloqueo `UPDLOCK/HOLDLOCK` (`409 Conflict` ante edición simultánea); validación binaria y decodificación real de imágenes JPG/PNG con `StbImageSharp 2.30.16` (máximo `10,000,000` bytes y 12 MP) almacenadas en `storage/catalogo` fuera de `wwwroot`; y desactivación mediante **baja lógica** (`activo = false`) conservando intactos pedidos e importes históricos.

### 5.4 CU-04 — Consultar catálogo de productos (`RF-03`)
- **Frontend público:** `src/frontend/catalogo.html`, `catalogo.css` y `catalogo.js` (tarjetas de variantes activas, búsqueda textual, filtros por talla/color/tela, enlace directo `catalogo.html?producto={id}` y diálogo accesible de detalle con cancelación de peticiones obsoletas).
- **Backend público:** `ProductosController` (`GET /api/productos`, `GET /api/productos/{id}`) y `ProductoPublicoDto`.
- **Reglas aplicadas:** Exposición exclusiva de variantes con `activo = true` (`404` si se desactiva o no existe), rotulación del importe como precio referencial, ocultamiento de `version` administrativa y bloqueo de cualquier ruta de imagen externa o privada.

---

## 6. Código preparatorio incorporado para la gestión administrativa de pedidos (en construcción)

Para adelantar la lógica de dominio y persistencia asignada a **Carlos** sin generar conflictos sobre `Program.cs` ni migraciones mientras se completan `CU-05`–`CU-08`, se incorporaron los siguientes componentes en `src/backend/ImpactoTextil.Api/`:

- **Contratos administrativos (`Contracts/AdministracionDtos.cs`):**
  - DTOs de entrada con validación `IValidatableObject` y rechazo de campos no mapeados: `FiltroPedidosAdminDto`, `ProponerCotizacionDto`, `LineaCotizacionPropuestaDto`, `DecidirCotizacionDto`, `RegistrarVersionDisenoTextoDto`, `DecidirVersionDisenoDto`, `ArchivarPedidoDto`, `ResolverPagoPendienteDto`, `RegistrarCobroDirectoDto` y `ActualizarEstadoProduccionDto`.
  - Records inmutables de respuesta para listados, expediente integral del pedido, resultados de cotización, diseño, archivo, pagos, comprobación de sublimación, cambio de estado y descarga de archivos.
- **Reglas de negocio puras (`Services/ReglasAdministrativasPedido.cs` y `ResultadoOperacionAdmin.cs`):**
  - Cálculo exacto de `anticipoExigible` al **50 %** con redondeo hacia arriba al centavo (`Math.Ceiling(totalAceptado * 50m) / 100m`), sin excepciones del 0 % ni 30 %.
  - Validación de importes `decimal(18,2)` rechazando más de dos decimales sin redondeo silencioso.
  - Secuencia estricta `Pendiente → En producción → Listo para entrega → Entregado`, bloqueo de avance en solicitudes `Archivadas`, verificación de aprobación vigente por cada `DetallePedido`, reevaluación obligatoria de la autorización de sublimación cuando aumenta el total aceptado durante `En producción`, validación de entrega con saldo `C$0.00` (identificada explícitamente como política de demostración académica pendiente de validación comercial) y validación de archivo administrativo (`Regla 13`).
- **Almacenamiento y validación de archivos privados (`Infrastructure/ArchivosPrivados.cs`):**
  - `ValidadorArchivosPrivados` y `AlmacenArchivosPrivadosLocal` con verificación de firmas binarias reales (`JPG`, `PNG` para diseños; `JPG`, `PNG`, `PDF` para comprobantes), tope inclusivo de `10,000,000` bytes, ubicación obligatoria fuera de `wwwroot` y protección contra *path traversal*.
- **Repositorio y servicios transaccionales (`Repositories/` y `Services/`):**
  - `IPedidoAdministracionRepository` y `PedidoAdministracionRepository`: consultas sobre las vistas `vPedido`, `vDetallePedido`, `vVersionCotizacion`, `vDetalleCotizacion` y `Constancia`, y envoltorio transaccional `EjecutarEnPedidoBloqueadoAsync` que adquiere `UPDLOCK, ROWLOCK, HOLDLOCK` sobre `dbo.Pedido` antes de releer importes y versiones.
  - `RevisionCotizacionAdminService` (`CU-10`), `DescargaDisenoAdminService` (`CU-11`), `PagosAdminService` (`CU-13`) y `ProduccionEntregaAdminService` (`CU-12`), alineados con el orden de persistencia exigido por los disparadores SQL (`TR_Pedido_versionPropia`, `TR_Detalle_versionPropia`, `TR_Pago_decisionUnica`, `TR_DisenoComprobado_pertenencia`) y con soporte de idempotencia ante reintentos.

---

## 7. Scripts operativos, empaquetado y validación ejecutada

- **Scripts disponibles en `scripts/`:**
  - `Iniciar-Local.ps1`: restaura herramientas, aplica migraciones a la base local `ImpactoTextil` e inicia la aplicación por HTTPS.
  - `Crear-Administrador.ps1`: ejecuta de forma interactiva la creación del primer Administrador local.
  - `Test-Modelo.ps1`: crea una base temporal aislada, aplica el modelo, corre las 29 pruebas SQL de `database/pruebas_modelo_fase3.sql` y elimina la base temporal.
  - `Test-Registro.ps1`: ejecuta la suite completa de pruebas automatizadas (`ImpactoTextil.Api.Tests`) contra SQL Server real.
  - `Preparar-Publicacion.ps1` y `Test-Publicacion.ps1`: generan entregas reproducibles (`sitio.zip`, `database/actualizar.sql`, `manifiesto.json`, `SHA256.csv`) excluyendo secretos y `storage/`, y ejecutan 14 comprobaciones públicas de lectura con TLS validado; documentados junto con [INSTALAR-MONSTERASP.md](../despliegue/INSTALAR-MONSTERASP.md).
- **Resultados de compilación y pruebas observados:**
  - En el cierre documentado de `CU-01`, `CU-02`, `CU-09` y `CU-04` con SQL Server local activo, la suite `ImpactoTextil.Api.Tests` registró **51 pruebas aprobadas, 0 fallidas y 0 omitidas**.
  - En la verificación ejecutada el **30 de octubre de 2026** con SDK `.NET 10.0.402`:
    - `dotnet build ImpactoTextil.slnx --no-restore`: **compilación correcta con 0 advertencias y 0 errores** (incluyendo los nuevos archivos preparatorios del bloque administrativo).
    - `dotnet test ImpactoTextil.slnx --no-build` (sin variable `IMPACTOTEXTIL_PRUEBAS_SQL`): **12 pruebas rápidas en memoria aprobadas, 0 fallidas y 39 pruebas `[PruebaSql]` omitidas** a la espera de correrse mediante `scripts/Test-Registro.ps1`.

---

## 8. Limitaciones actuales y trabajo pendiente

1. **Recorrido de cliente (`CU-05`, `CU-06`, `CU-07` y `CU-08` — responsabilidad de Dylan):** Pendientes de implementación en frontend, API, guardado conjunto de solicitud con archivos privados, carga de comprobantes `Pendientes` y consulta de estado/saldo de pedidos propios.
2. **Cierre integral del recorrido administrativo (`CU-10`, `CU-11`, `CU-12` y `CU-13` — responsabilidad de Carlos):** Falta registrar los nuevos servicios y `IAlmacenArchivosPrivados` en `Program.cs` de forma coordinada, crear los controladores HTTP protegidos con `[Authorize(Policy = "Administrador")]` y CSRF, construir las pantallas administrativas responsivas (320 px y escritorio) y escribir/ejecutar sus pruebas unitarias y `[PruebaSql]` de concurrencia, duplicados y permisos.
3. **Revisión cruzada e integración en `main`:** Los avances continúan en trabajo local/rama personal; requieren revisión humana entre compañeros y fusión ordenada mediante Pull Requests.
4. **Alojamiento y pruebas no funcionales en destino real:** Aún no se dispone de la cuenta en **MonsterASP.NET Free**, por lo que siguen pendientes el despliegue público, la activación de HTTPS con soporte del proveedor y la ejecución formal de los protocolos `RNF-02` (carga de imágenes < 3 s con red), `RNF-05` (50 usuarios concurrentes), `RNF-06` (observación mensual de disponibilidad), `RNF-08` (matriz de navegadores de escritorio y móvil) y `RNF-09` (documentación de revisión de Ley 787).
5. **Confirmaciones externas abiertas en el SRS:** Validación comercial por los propietarios de la política de entrega únicamente con saldo `C$0.00` y de longitudes/opciones propuestas, así como confirmación con el docente de la fecha definitiva de defensa y del diferimiento declarado de `CU-03`.

