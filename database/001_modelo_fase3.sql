/* Impacto Textil: prototipo relacional de Fase 3.
   Ejecutar SOLO en una base vacía de desarrollo seleccionada por el operador.
   No crea ni elimina bases, no incorpora credenciales y no modifica el DOCX.
   La migración EF Core definitiva se generará después de la revisión de diseño.
   SQL Server: tablas normalizadas, vistas para los atributos lógicos calculados.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF OBJECT_ID(N'dbo.Usuario', N'U') IS NOT NULL
    THROW 51000, N'El prototipo requiere una base vacía; no se sobrescribe un esquema existente.', 1;
GO
BEGIN TRANSACTION;

CREATE TABLE dbo.Usuario (
    id int IDENTITY PRIMARY KEY,
    nombre nvarchar(100) NOT NULL,
    correo nvarchar(150) COLLATE Latin1_General_100_CI_AS_SC NOT NULL,
    passwordHash varchar(60) NOT NULL,
    rol nvarchar(20) NOT NULL,
    activo bit NOT NULL CONSTRAINT DF_Usuario_activo DEFAULT 1,
    aceptacionTerminos bit NOT NULL,
    fechaAceptacion datetimeoffset(0) NULL,
    CONSTRAINT UQ_Usuario_correo UNIQUE (correo),
    CONSTRAINT CK_Usuario_nombre CHECK (LEN(TRIM(nombre)) > 0),
    CONSTRAINT CK_Usuario_correo CHECK (LEN(TRIM(correo)) > 0 AND DATALENGTH(correo) = DATALENGTH(TRIM(correo))),
    CONSTRAINT CK_Usuario_rol CHECK (rol IN (N'Cliente', N'Administrador')),
    CONSTRAINT CK_Usuario_hash CHECK (LEN(passwordHash) = 60 AND LEFT(passwordHash, 4) IN ('$2a$', '$2b$', '$2y$')),
    CONSTRAINT CK_Usuario_consentimiento CHECK (
        (aceptacionTerminos = 1 AND fechaAceptacion IS NOT NULL) OR
        (aceptacionTerminos = 0 AND fechaAceptacion IS NULL AND rol = N'Administrador'))
);

CREATE TABLE dbo.Producto (
    id int IDENTITY PRIMARY KEY,
    nombre nvarchar(100) NOT NULL,
    color nvarchar(50) NOT NULL,
    talla nvarchar(30) NOT NULL,
    tela nvarchar(100) NOT NULL,
    imagenURL nvarchar(2048) NOT NULL,
    precioBase decimal(18,2) NOT NULL,
    activo bit NOT NULL CONSTRAINT DF_Producto_activo DEFAULT 1,
    CONSTRAINT CK_Producto_textos CHECK (LEN(TRIM(nombre)) > 0 AND LEN(TRIM(color)) > 0 AND LEN(TRIM(talla)) > 0 AND LEN(TRIM(tela)) > 0 AND LEN(TRIM(imagenURL)) > 0),
    CONSTRAINT CK_Producto_precio CHECK (precioBase > 0)
);

CREATE TABLE dbo.Pedido (
    id int IDENTITY PRIMARY KEY,
    codigo nvarchar(50) NOT NULL,
    cliente int NOT NULL REFERENCES dbo.Usuario(id),
    fechaOrden datetimeoffset(0) NOT NULL,
    fechaEntregaAcordada date NULL,
    fechaEntregaReal datetimeoffset(0) NULL,
    estado nvarchar(30) NOT NULL CONSTRAINT DF_Pedido_estado DEFAULT N'Pendiente',
    modalidadEntrega nvarchar(15) NOT NULL,
    direccionEntrega nvarchar(250) NULL,
    versionCotizacionVigente int NULL,
    condicionAdministrativa nvarchar(10) NOT NULL CONSTRAINT DF_Pedido_condicion DEFAULT N'Activa',
    motivoArchivo nvarchar(250) NULL,
    fechaArchivo datetimeoffset(0) NULL,
    responsableArchivo int NULL REFERENCES dbo.Usuario(id),
    confirmacion uniqueidentifier NOT NULL,
    huellaSolicitud binary(32) NOT NULL,
    controlConcurrencia rowversion NOT NULL,
    CONSTRAINT UQ_Pedido_codigo UNIQUE (codigo),
    CONSTRAINT UQ_Pedido_confirmacion UNIQUE (cliente, confirmacion),
    CONSTRAINT CK_Pedido_codigo CHECK (LEN(TRIM(codigo)) > 0),
    CONSTRAINT CK_Pedido_estado CHECK (estado IN (N'Pendiente', N'En producción', N'Listo para entrega', N'Entregado')),
    CONSTRAINT CK_Pedido_modalidad CHECK (modalidadEntrega IN (N'Retiro', N'Domicilio')),
    CONSTRAINT CK_Pedido_direccion CHECK (modalidadEntrega = N'Retiro' OR (direccionEntrega IS NOT NULL AND LEN(TRIM(direccionEntrega)) > 0)),
    CONSTRAINT CK_Pedido_entrega CHECK ((estado = N'Entregado' AND fechaEntregaReal IS NOT NULL) OR (estado <> N'Entregado' AND fechaEntregaReal IS NULL)),
    CONSTRAINT CK_Pedido_archivo CHECK (
        (condicionAdministrativa = N'Activa' AND motivoArchivo IS NULL AND fechaArchivo IS NULL AND responsableArchivo IS NULL) OR
        (condicionAdministrativa = N'Archivada' AND estado = N'Pendiente' AND versionCotizacionVigente IS NULL AND motivoArchivo IS NOT NULL AND LEN(TRIM(motivoArchivo)) > 0 AND fechaArchivo IS NOT NULL AND responsableArchivo IS NOT NULL))
);
CREATE INDEX IX_Pedido_cliente_fecha ON dbo.Pedido(cliente, fechaOrden);

CREATE TABLE dbo.DetallePedido (
    id int IDENTITY PRIMARY KEY,
    pedido int NOT NULL REFERENCES dbo.Pedido(id),
    producto int NOT NULL REFERENCES dbo.Producto(id),
    cantidad int NOT NULL,
    talla nvarchar(30) NOT NULL,
    color nvarchar(50) NOT NULL,
    tela nvarchar(100) NOT NULL,
    empaque nvarchar(100) NOT NULL,
    observaciones nvarchar(1000) NULL,
    ideaDiseno nvarchar(1000) NULL,
    archivoDisenoURL nvarchar(2048) NULL,
    precioBaseReferencia decimal(18,2) NOT NULL,
    versionDisenoVigente int NULL,
    CONSTRAINT CK_Detalle_cantidad CHECK (cantidad > 0),
    CONSTRAINT CK_Detalle_precioReferencia CHECK (precioBaseReferencia > 0),
    CONSTRAINT CK_Detalle_textos CHECK (LEN(TRIM(talla)) > 0 AND LEN(TRIM(color)) > 0 AND LEN(TRIM(tela)) > 0 AND LEN(TRIM(empaque)) > 0),
    CONSTRAINT CK_Detalle_diseno CHECK ((archivoDisenoURL IS NOT NULL AND LEN(TRIM(archivoDisenoURL)) > 0) OR (ideaDiseno IS NOT NULL AND LEN(TRIM(ideaDiseno)) > 0))
);
CREATE INDEX IX_DetallePedido_pedido ON dbo.DetallePedido(pedido);

CREATE TABLE dbo.VersionDiseno (
    id int IDENTITY PRIMARY KEY,
    numeroVersion int NOT NULL,
    detalle int NOT NULL REFERENCES dbo.DetallePedido(id),
    archivoURL nvarchar(2048) NULL,
    descripcion nvarchar(1000) NULL,
    fechaRegistro datetimeoffset(0) NOT NULL,
    responsableRegistro int NOT NULL REFERENCES dbo.Usuario(id),
    decision nvarchar(10) NOT NULL CONSTRAINT DF_VersionDiseno_decision DEFAULT N'Pendiente',
    fechaDecision datetimeoffset(0) NULL,
    responsableDecision int NULL REFERENCES dbo.Usuario(id),
    referenciaDecision nvarchar(250) NULL,
    CONSTRAINT UQ_VersionDiseno_numero UNIQUE (detalle, numeroVersion),
    CONSTRAINT CK_VersionDiseno_numero CHECK (numeroVersion > 0),
    CONSTRAINT CK_VersionDiseno_contenido CHECK ((archivoURL IS NOT NULL AND LEN(TRIM(archivoURL)) > 0) OR (descripcion IS NOT NULL AND LEN(TRIM(descripcion)) > 0)),
    CONSTRAINT CK_VersionDiseno_decision CHECK (
        (decision = N'Pendiente' AND fechaDecision IS NULL AND responsableDecision IS NULL AND referenciaDecision IS NULL) OR
        (decision IN (N'Aprobada', N'Rechazada') AND fechaDecision IS NOT NULL AND fechaDecision >= fechaRegistro AND responsableDecision IS NOT NULL AND referenciaDecision IS NOT NULL AND LEN(TRIM(referenciaDecision)) > 0))
);

CREATE TABLE dbo.VersionCotizacion (
    id int IDENTITY PRIMARY KEY,
    numeroVersion int NOT NULL,
    pedido int NOT NULL REFERENCES dbo.Pedido(id),
    fechaRegistro datetimeoffset(0) NOT NULL,
    responsableRegistro int NOT NULL REFERENCES dbo.Usuario(id),
    decision nvarchar(10) NOT NULL CONSTRAINT DF_VersionCotizacion_decision DEFAULT N'Propuesta',
    fechaDecision datetimeoffset(0) NULL,
    responsableDecision int NULL REFERENCES dbo.Usuario(id),
    referenciaDecision nvarchar(250) NULL,
    CONSTRAINT UQ_VersionCotizacion_numero UNIQUE (pedido, numeroVersion),
    CONSTRAINT CK_VersionCotizacion_numero CHECK (numeroVersion > 0),
    CONSTRAINT CK_VersionCotizacion_decision CHECK (
        (decision = N'Propuesta' AND fechaDecision IS NULL AND responsableDecision IS NULL AND referenciaDecision IS NULL) OR
        (decision IN (N'Aceptada', N'Rechazada') AND fechaDecision IS NOT NULL AND fechaDecision >= fechaRegistro AND responsableDecision IS NOT NULL AND referenciaDecision IS NOT NULL AND LEN(TRIM(referenciaDecision)) > 0))
);

CREATE TABLE dbo.DetalleCotizacion (
    versionCotizacion int NOT NULL REFERENCES dbo.VersionCotizacion(id),
    detalle int NOT NULL REFERENCES dbo.DetallePedido(id),
    precioUnitarioAcordado decimal(18,2) NOT NULL,
    versionDiseno int NULL REFERENCES dbo.VersionDiseno(id),
    CONSTRAINT PK_DetalleCotizacion PRIMARY KEY (versionCotizacion, detalle),
    CONSTRAINT CK_DetalleCotizacion_importes CHECK (precioUnitarioAcordado > 0)
);

CREATE TABLE dbo.Pago (
    id int IDENTITY PRIMARY KEY,
    pedido int NOT NULL REFERENCES dbo.Pedido(id),
    monto decimal(18,2) NOT NULL,
    fecha datetimeoffset(0) NOT NULL,
    medio nvarchar(20) NOT NULL,
    referencia nvarchar(100) NULL,
    estadoVerificacion nvarchar(10) NOT NULL CONSTRAINT DF_Pago_estado DEFAULT N'Pendiente',
    comprobanteURL nvarchar(2048) NULL,
    fechaVerificacion datetimeoffset(0) NULL,
    responsableVerificacion int NULL REFERENCES dbo.Usuario(id),
    confirmacion uniqueidentifier NOT NULL,
    controlConcurrencia rowversion NOT NULL,
    CONSTRAINT UQ_Pago_confirmacion UNIQUE (pedido, confirmacion),
    CONSTRAINT CK_Pago_monto CHECK (monto > 0),
    CONSTRAINT CK_Pago_medio CHECK (medio IN (N'Efectivo', N'Transferencia', N'Depósito', N'Tarjeta')),
    CONSTRAINT CK_Pago_resolucion CHECK (
        (estadoVerificacion = N'Pendiente' AND fechaVerificacion IS NULL AND responsableVerificacion IS NULL AND comprobanteURL IS NOT NULL AND LEN(TRIM(comprobanteURL)) > 0) OR
        (estadoVerificacion IN (N'Verificado', N'Rechazado') AND fechaVerificacion IS NOT NULL AND responsableVerificacion IS NOT NULL)),
    CONSTRAINT CK_Pago_referencia CHECK (estadoVerificacion <> N'Verificado' OR (referencia IS NOT NULL AND LEN(TRIM(referencia)) > 0)),
    CONSTRAINT CK_Pago_respaldo CHECK ((comprobanteURL IS NOT NULL AND LEN(TRIM(comprobanteURL)) > 0) OR (estadoVerificacion = N'Verificado' AND referencia IS NOT NULL AND LEN(TRIM(referencia)) > 0))
);
CREATE UNIQUE INDEX UX_Pago_referenciaVerificada ON dbo.Pago(pedido, medio, referencia) WHERE estadoVerificacion = N'Verificado';
CREATE INDEX IX_Pago_pedido_estado ON dbo.Pago(pedido, estadoVerificacion) INCLUDE (monto);

CREATE TABLE dbo.ConstanciaEntrega (
    id int IDENTITY PRIMARY KEY,
    pedido int NOT NULL REFERENCES dbo.Pedido(id),
    decision bit NOT NULL,
    fecha datetimeoffset(0) NOT NULL,
    responsable int NOT NULL REFERENCES dbo.Usuario(id),
    referencia nvarchar(250) NOT NULL,
    CONSTRAINT CK_ConstanciaEntrega_referencia CHECK (LEN(TRIM(referencia)) > 0)
);

CREATE TABLE dbo.ComprobacionSublimacion (
    id int IDENTITY PRIMARY KEY,
    versionCotizacion int NOT NULL REFERENCES dbo.VersionCotizacion(id),
    fecha datetimeoffset(0) NOT NULL,
    responsable int NOT NULL REFERENCES dbo.Usuario(id),
    montoVerificado decimal(28,2) NOT NULL,
    CONSTRAINT CK_Comprobacion_monto CHECK (montoVerificado >= 0)
);
CREATE TABLE dbo.DisenoComprobado (
    comprobacion int NOT NULL REFERENCES dbo.ComprobacionSublimacion(id),
    versionDiseno int NOT NULL REFERENCES dbo.VersionDiseno(id),
    CONSTRAINT PK_DisenoComprobado PRIMARY KEY (comprobacion, versionDiseno)
);

ALTER TABLE dbo.Pedido ADD CONSTRAINT FK_Pedido_versionVigente FOREIGN KEY (versionCotizacionVigente) REFERENCES dbo.VersionCotizacion(id);
ALTER TABLE dbo.DetallePedido ADD CONSTRAINT FK_Detalle_versionVigente FOREIGN KEY (versionDisenoVigente) REFERENCES dbo.VersionDiseno(id);
COMMIT;
GO

CREATE VIEW dbo.vDetalleCotizacion AS
SELECT c.*, d.cantidad,
       CAST(d.cantidad * c.precioUnitarioAcordado AS decimal(28,2)) AS subtotal
FROM dbo.DetalleCotizacion c JOIN dbo.DetallePedido d ON d.id = c.detalle;
GO
CREATE VIEW dbo.vVersionCotizacion AS
SELECT v.*, t.total
FROM dbo.VersionCotizacion v
OUTER APPLY (SELECT CAST(SUM(d.subtotal) AS decimal(28,2)) AS total
             FROM dbo.vDetalleCotizacion d WHERE d.versionCotizacion = v.id) t;
GO
CREATE VIEW dbo.vPedido AS
SELECT p.*, r.totalReferencial, v.total,
       CAST(50.00 AS decimal(5,2)) AS porcentajeAnticipo,
       CAST(CEILING(v.total * 50) / 100 AS decimal(28,2)) AS anticipoExigible,
       CAST(v.total - COALESCE(a.verificado, 0) AS decimal(28,2)) AS saldoPendiente,
       CAST(CASE WHEN p.versionCotizacionVigente IS NULL THEN 0 ELSE 1 END AS bit) AS cotizacionAceptada,
       CAST(0 AS bit) AS excepcionAnticipo
FROM dbo.Pedido p
LEFT JOIN dbo.vVersionCotizacion v ON v.id = p.versionCotizacionVigente
OUTER APPLY (SELECT CAST(SUM(CAST(d.cantidad AS decimal(10,0)) * d.precioBaseReferencia) AS decimal(28,2)) AS totalReferencial
             FROM dbo.DetallePedido d WHERE d.pedido = p.id) r
OUTER APPLY (SELECT SUM(g.monto) AS verificado FROM dbo.Pago g WHERE g.pedido = p.id AND g.estadoVerificacion = N'Verificado') a;
GO
CREATE VIEW dbo.vDetallePedido AS
SELECT d.*, c.precioUnitarioAcordado,
       CAST(c.cantidad * c.precioUnitarioAcordado AS decimal(28,2)) AS subtotal,
       CAST(CASE WHEN v.id IS NULL THEN 0 ELSE 1 END AS bit) AS disenoAprobado,
       v.fechaDecision AS fechaAprobacionDiseno,
       v.responsableDecision AS responsableAprobacionDiseno
FROM dbo.DetallePedido d
JOIN dbo.Pedido p ON p.id = d.pedido
LEFT JOIN dbo.vDetalleCotizacion c ON c.versionCotizacion = p.versionCotizacionVigente AND c.detalle = d.id
LEFT JOIN dbo.VersionDiseno v ON v.id = d.versionDisenoVigente;
GO
CREATE VIEW dbo.Constancia AS
SELECT CAST(N'Cotización' AS nvarchar(15)) AS tipo, v.id AS registroOrigen, v.pedido,
       CAST(CASE WHEN v.decision = N'Aceptada' THEN 1 ELSE 0 END AS bit) AS decision,
       v.fechaDecision AS fecha, v.responsableDecision AS responsable,
       v.referenciaDecision AS referencia, v.id AS versionCotizacion
FROM dbo.VersionCotizacion v WHERE v.decision IN (N'Aceptada', N'Rechazada')
UNION ALL
SELECT N'Entrega', e.id, e.pedido, e.decision, e.fecha, e.responsable, e.referencia, CAST(NULL AS int)
FROM dbo.ConstanciaEntrega e;
GO

CREATE TRIGGER dbo.TR_Pedido_versionPropia ON dbo.Pedido AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted p JOIN dbo.VersionCotizacion v ON v.id = p.versionCotizacionVigente
               WHERE v.pedido <> p.id OR v.decision <> N'Aceptada')
        THROW 51001, N'La cotización vigente debe pertenecer al pedido y estar aceptada.', 1;
END;
GO
CREATE TRIGGER dbo.TR_Detalle_versionPropia ON dbo.DetallePedido AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted d JOIN dbo.VersionDiseno v ON v.id = d.versionDisenoVigente
               WHERE v.detalle <> d.id OR v.decision <> N'Aprobada')
        THROW 51002, N'El diseño vigente debe pertenecer al detalle y estar aprobado.', 1;
END;
GO
CREATE TRIGGER dbo.TR_DetalleCotizacion_pertenencia ON dbo.DetalleCotizacion AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.VersionCotizacion v ON v.id = i.versionCotizacion
               JOIN dbo.DetallePedido d ON d.id = i.detalle LEFT JOIN dbo.VersionDiseno a ON a.id = i.versionDiseno
               WHERE v.pedido <> d.pedido OR v.decision <> N'Propuesta' OR (a.id IS NOT NULL AND a.detalle <> d.id))
        THROW 51003, N'Los importes y diseños deben corresponder al pedido de una cotización propuesta.', 1;
END;
GO
CREATE TRIGGER dbo.TR_DetalleCotizacion_inmutable ON dbo.DetalleCotizacion AFTER UPDATE, DELETE AS
BEGIN
    THROW 51004, N'Los importes históricos no se sobrescriben; crear otra versión.', 1;
END;
GO
CREATE TRIGGER dbo.TR_VersionDiseno_inmutable ON dbo.VersionDiseno AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.decision <> N'Pendiente')
        THROW 51005, N'No se elimina ni se vuelve a decidir una versión de diseño.', 1;
    IF EXISTS (SELECT id, numeroVersion, detalle, archivoURL, descripcion, fechaRegistro, responsableRegistro FROM deleted
               EXCEPT SELECT id, numeroVersion, detalle, archivoURL, descripcion, fechaRegistro, responsableRegistro FROM inserted)
        THROW 51006, N'Una revisión del contenido requiere otra versión de diseño.', 1;
END;
GO
CREATE TRIGGER dbo.TR_VersionCotizacion_inmutable ON dbo.VersionCotizacion AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.decision <> N'Propuesta')
        THROW 51007, N'No se elimina ni se vuelve a decidir una versión de cotización.', 1;
    IF EXISTS (SELECT id, numeroVersion, pedido, fechaRegistro, responsableRegistro FROM deleted
               EXCEPT SELECT id, numeroVersion, pedido, fechaRegistro, responsableRegistro FROM inserted)
        THROW 51008, N'Una revisión requiere otra versión de cotización.', 1;
END;
GO
CREATE TRIGGER dbo.TR_DisenoComprobado_pertenencia ON dbo.DisenoComprobado AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ComprobacionSublimacion c ON c.id = i.comprobacion
               JOIN dbo.VersionCotizacion q ON q.id = c.versionCotizacion
               JOIN dbo.VersionDiseno v ON v.id = i.versionDiseno JOIN dbo.DetallePedido d ON d.id = v.detalle
               WHERE d.pedido <> q.pedido OR v.decision <> N'Aprobada' OR q.decision <> N'Aceptada')
        THROW 51009, N'La comprobación requiere versiones aprobadas del mismo pedido.', 1;
    IF EXISTS (SELECT c.comprobacion, v.detalle FROM dbo.DisenoComprobado c JOIN dbo.VersionDiseno v ON v.id = c.versionDiseno
               WHERE c.comprobacion IN (SELECT comprobacion FROM inserted) GROUP BY c.comprobacion, v.detalle HAVING COUNT(*) > 1)
        THROW 51010, N'Una comprobación registra una sola versión por detalle.', 1;
END;
GO
CREATE TRIGGER dbo.TR_Pago_decisionUnica ON dbo.Pago AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.estadoVerificacion <> N'Pendiente')
        THROW 51011, N'Un pago resuelto conserva su decisión e importe; no se elimina ni se vuelve a acreditar.', 1;
END;
GO
CREATE TRIGGER dbo.TR_Detalle_solicitudHistorica ON dbo.DetallePedido AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT id,pedido,producto,cantidad,talla,color,tela,empaque,observaciones,ideaDiseno,archivoDisenoURL,precioBaseReferencia FROM deleted
               EXCEPT SELECT id,pedido,producto,cantidad,talla,color,tela,empaque,observaciones,ideaDiseno,archivoDisenoURL,precioBaseReferencia FROM inserted)
        THROW 51015, N'La solicitud original se conserva; los cambios de diseño se registran en sus versiones.', 1;
END;
GO
CREATE TRIGGER dbo.TR_ConstanciaEntrega_inmutable ON dbo.ConstanciaEntrega AFTER UPDATE, DELETE AS
BEGIN
    THROW 51012, N'Las constancias conservan su contenido; una nueva decisión requiere otro registro.', 1;
END;
GO
CREATE TRIGGER dbo.TR_Comprobacion_inmutable ON dbo.ComprobacionSublimacion AFTER UPDATE, DELETE AS
BEGIN
    THROW 51013, N'Las comprobaciones conservan el contexto histórico de versiones y pagos.', 1;
END;
GO
CREATE TRIGGER dbo.TR_DisenoComprobado_inmutable ON dbo.DisenoComprobado AFTER UPDATE, DELETE AS
BEGIN
    THROW 51014, N'Los diseños de una comprobación histórica no se sobrescriben.', 1;
END;
GO
