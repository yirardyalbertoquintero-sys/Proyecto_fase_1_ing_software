IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [Producto] (
        [id] int NOT NULL IDENTITY,
        [nombre] nvarchar(100) NOT NULL,
        [color] nvarchar(50) NOT NULL,
        [talla] nvarchar(30) NOT NULL,
        [tela] nvarchar(100) NOT NULL,
        [imagenURL] nvarchar(2048) NOT NULL,
        [precioBase] decimal(18,2) NOT NULL,
        [activo] bit NOT NULL CONSTRAINT [DF_Producto_activo] DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Producto] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Producto_precio] CHECK (([precioBase]>(0))),
        CONSTRAINT [CK_Producto_textos] CHECK ((len(Trim([nombre]))>(0) AND len(Trim([color]))>(0) AND len(Trim([talla]))>(0) AND len(Trim([tela]))>(0) AND len(Trim([imagenURL]))>(0)))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [Usuario] (
        [id] int NOT NULL IDENTITY,
        [nombre] nvarchar(100) NOT NULL,
        [correo] nvarchar(150) COLLATE Latin1_General_100_CI_AS_SC NOT NULL,
        [passwordHash] varchar(60) NOT NULL,
        [rol] nvarchar(20) NOT NULL,
        [activo] bit NOT NULL CONSTRAINT [DF_Usuario_activo] DEFAULT CAST(1 AS bit),
        [aceptacionTerminos] bit NOT NULL,
        [fechaAceptacion] datetimeoffset(0) NULL,
        CONSTRAINT [PK_Usuario] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Usuario_consentimiento] CHECK (([aceptacionTerminos]=(1) AND [fechaAceptacion] IS NOT NULL OR [aceptacionTerminos]=(0) AND [fechaAceptacion] IS NULL AND [rol]=N'Administrador')),
        CONSTRAINT [CK_Usuario_correo] CHECK ((len(Trim([correo]))>(0) AND datalength([correo])=datalength(Trim([correo])))),
        CONSTRAINT [CK_Usuario_hash] CHECK ((len([passwordHash])=(60) AND (left([passwordHash],(4))='$2y$' OR left([passwordHash],(4))='$2b$' OR left([passwordHash],(4))='$2a$'))),
        CONSTRAINT [CK_Usuario_nombre] CHECK ((len(Trim([nombre]))>(0))),
        CONSTRAINT [CK_Usuario_rol] CHECK (([rol]=N'Administrador' OR [rol]=N'Cliente'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [ComprobacionSublimacion] (
        [id] int NOT NULL IDENTITY,
        [versionCotizacion] int NOT NULL,
        [fecha] datetimeoffset(0) NOT NULL,
        [responsable] int NOT NULL,
        [montoVerificado] decimal(28,2) NOT NULL,
        CONSTRAINT [PK_ComprobacionSublimacion] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Comprobacion_monto] CHECK (([montoVerificado]>=(0))),
        CONSTRAINT [FK_ComprobacionSublimacion_Usuario_responsable] FOREIGN KEY ([responsable]) REFERENCES [Usuario] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [ConstanciaEntrega] (
        [id] int NOT NULL IDENTITY,
        [pedido] int NOT NULL,
        [decision] bit NOT NULL,
        [fecha] datetimeoffset(0) NOT NULL,
        [responsable] int NOT NULL,
        [referencia] nvarchar(250) NOT NULL,
        CONSTRAINT [PK_ConstanciaEntrega] PRIMARY KEY ([id]),
        CONSTRAINT [CK_ConstanciaEntrega_referencia] CHECK ((len(Trim([referencia]))>(0))),
        CONSTRAINT [FK_ConstanciaEntrega_Usuario_responsable] FOREIGN KEY ([responsable]) REFERENCES [Usuario] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [DetalleCotizacion] (
        [versionCotizacion] int NOT NULL,
        [detalle] int NOT NULL,
        [precioUnitarioAcordado] decimal(18,2) NOT NULL,
        [versionDiseno] int NULL,
        CONSTRAINT [PK_DetalleCotizacion] PRIMARY KEY ([versionCotizacion], [detalle]),
        CONSTRAINT [CK_DetalleCotizacion_importes] CHECK (([precioUnitarioAcordado]>(0)))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [DetallePedido] (
        [id] int NOT NULL IDENTITY,
        [pedido] int NOT NULL,
        [producto] int NOT NULL,
        [cantidad] int NOT NULL,
        [talla] nvarchar(30) NOT NULL,
        [color] nvarchar(50) NOT NULL,
        [tela] nvarchar(100) NOT NULL,
        [empaque] nvarchar(100) NOT NULL,
        [observaciones] nvarchar(1000) NULL,
        [ideaDiseno] nvarchar(1000) NULL,
        [archivoDisenoURL] nvarchar(2048) NULL,
        [precioBaseReferencia] decimal(18,2) NOT NULL,
        [versionDisenoVigente] int NULL,
        CONSTRAINT [PK_DetallePedido] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Detalle_cantidad] CHECK (([cantidad]>(0))),
        CONSTRAINT [CK_Detalle_diseno] CHECK (([archivoDisenoURL] IS NOT NULL AND len(Trim([archivoDisenoURL]))>(0) OR [ideaDiseno] IS NOT NULL AND len(Trim([ideaDiseno]))>(0))),
        CONSTRAINT [CK_Detalle_precioReferencia] CHECK (([precioBaseReferencia]>(0))),
        CONSTRAINT [CK_Detalle_textos] CHECK ((len(Trim([talla]))>(0) AND len(Trim([color]))>(0) AND len(Trim([tela]))>(0) AND len(Trim([empaque]))>(0))),
        CONSTRAINT [FK_DetallePedido_Producto_producto] FOREIGN KEY ([producto]) REFERENCES [Producto] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [VersionDiseno] (
        [id] int NOT NULL IDENTITY,
        [numeroVersion] int NOT NULL,
        [detalle] int NOT NULL,
        [archivoURL] nvarchar(2048) NULL,
        [descripcion] nvarchar(1000) NULL,
        [fechaRegistro] datetimeoffset(0) NOT NULL,
        [responsableRegistro] int NOT NULL,
        [decision] nvarchar(10) NOT NULL CONSTRAINT [DF_VersionDiseno_decision] DEFAULT N'Pendiente',
        [fechaDecision] datetimeoffset(0) NULL,
        [responsableDecision] int NULL,
        [referenciaDecision] nvarchar(250) NULL,
        CONSTRAINT [PK_VersionDiseno] PRIMARY KEY ([id]),
        CONSTRAINT [CK_VersionDiseno_contenido] CHECK (([archivoURL] IS NOT NULL AND len(Trim([archivoURL]))>(0) OR [descripcion] IS NOT NULL AND len(Trim([descripcion]))>(0))),
        CONSTRAINT [CK_VersionDiseno_decision] CHECK (([decision]=N'Pendiente' AND [fechaDecision] IS NULL AND [responsableDecision] IS NULL AND [referenciaDecision] IS NULL OR ([decision]=N'Rechazada' OR [decision]=N'Aprobada') AND [fechaDecision] IS NOT NULL AND [fechaDecision]>=[fechaRegistro] AND [responsableDecision] IS NOT NULL AND [referenciaDecision] IS NOT NULL AND len(Trim([referenciaDecision]))>(0))),
        CONSTRAINT [CK_VersionDiseno_numero] CHECK (([numeroVersion]>(0))),
        CONSTRAINT [FK_VersionDiseno_DetallePedido_detalle] FOREIGN KEY ([detalle]) REFERENCES [DetallePedido] ([id]),
        CONSTRAINT [FK_VersionDiseno_Usuario_responsableDecision] FOREIGN KEY ([responsableDecision]) REFERENCES [Usuario] ([id]),
        CONSTRAINT [FK_VersionDiseno_Usuario_responsableRegistro] FOREIGN KEY ([responsableRegistro]) REFERENCES [Usuario] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [DisenoComprobado] (
        [comprobacion] int NOT NULL,
        [versionDiseno] int NOT NULL,
        CONSTRAINT [PK_DisenoComprobado] PRIMARY KEY ([comprobacion], [versionDiseno]),
        CONSTRAINT [FK_DisenoComprobado_ComprobacionSublimacion_comprobacion] FOREIGN KEY ([comprobacion]) REFERENCES [ComprobacionSublimacion] ([id]),
        CONSTRAINT [FK_DisenoComprobado_VersionDiseno_versionDiseno] FOREIGN KEY ([versionDiseno]) REFERENCES [VersionDiseno] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [Pago] (
        [id] int NOT NULL IDENTITY,
        [pedido] int NOT NULL,
        [monto] decimal(18,2) NOT NULL,
        [fecha] datetimeoffset(0) NOT NULL,
        [medio] nvarchar(20) NOT NULL,
        [referencia] nvarchar(100) NULL,
        [estadoVerificacion] nvarchar(10) NOT NULL CONSTRAINT [DF_Pago_estado] DEFAULT N'Pendiente',
        [comprobanteURL] nvarchar(2048) NULL,
        [fechaVerificacion] datetimeoffset(0) NULL,
        [responsableVerificacion] int NULL,
        [confirmacion] uniqueidentifier NOT NULL,
        [controlConcurrencia] rowversion NOT NULL,
        CONSTRAINT [PK_Pago] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Pago_medio] CHECK (([medio]=N'Tarjeta' OR [medio]=N'Depósito' OR [medio]=N'Transferencia' OR [medio]=N'Efectivo')),
        CONSTRAINT [CK_Pago_monto] CHECK (([monto]>(0))),
        CONSTRAINT [CK_Pago_referencia] CHECK (([estadoVerificacion]<>N'Verificado' OR [referencia] IS NOT NULL AND len(Trim([referencia]))>(0))),
        CONSTRAINT [CK_Pago_resolucion] CHECK (([estadoVerificacion]=N'Pendiente' AND [fechaVerificacion] IS NULL AND [responsableVerificacion] IS NULL AND [comprobanteURL] IS NOT NULL AND len(Trim([comprobanteURL]))>(0) OR ([estadoVerificacion]=N'Rechazado' OR [estadoVerificacion]=N'Verificado') AND [fechaVerificacion] IS NOT NULL AND [responsableVerificacion] IS NOT NULL)),
        CONSTRAINT [CK_Pago_respaldo] CHECK (([comprobanteURL] IS NOT NULL AND len(Trim([comprobanteURL]))>(0) OR [estadoVerificacion]=N'Verificado' AND [referencia] IS NOT NULL AND len(Trim([referencia]))>(0))),
        CONSTRAINT [FK_Pago_Usuario_responsableVerificacion] FOREIGN KEY ([responsableVerificacion]) REFERENCES [Usuario] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [Pedido] (
        [id] int NOT NULL IDENTITY,
        [codigo] nvarchar(50) NOT NULL,
        [cliente] int NOT NULL,
        [fechaOrden] datetimeoffset(0) NOT NULL,
        [fechaEntregaAcordada] date NULL,
        [fechaEntregaReal] datetimeoffset(0) NULL,
        [estado] nvarchar(30) NOT NULL CONSTRAINT [DF_Pedido_estado] DEFAULT N'Pendiente',
        [modalidadEntrega] nvarchar(15) NOT NULL,
        [direccionEntrega] nvarchar(250) NULL,
        [versionCotizacionVigente] int NULL,
        [condicionAdministrativa] nvarchar(10) NOT NULL CONSTRAINT [DF_Pedido_condicion] DEFAULT N'Activa',
        [motivoArchivo] nvarchar(250) NULL,
        [fechaArchivo] datetimeoffset(0) NULL,
        [responsableArchivo] int NULL,
        [confirmacion] uniqueidentifier NOT NULL,
        [huellaSolicitud] binary(32) NOT NULL,
        [controlConcurrencia] rowversion NOT NULL,
        CONSTRAINT [PK_Pedido] PRIMARY KEY ([id]),
        CONSTRAINT [CK_Pedido_archivo] CHECK (([condicionAdministrativa]=N'Activa' AND [motivoArchivo] IS NULL AND [fechaArchivo] IS NULL AND [responsableArchivo] IS NULL OR [condicionAdministrativa]=N'Archivada' AND [estado]=N'Pendiente' AND [versionCotizacionVigente] IS NULL AND [motivoArchivo] IS NOT NULL AND len(Trim([motivoArchivo]))>(0) AND [fechaArchivo] IS NOT NULL AND [responsableArchivo] IS NOT NULL)),
        CONSTRAINT [CK_Pedido_codigo] CHECK ((len(Trim([codigo]))>(0))),
        CONSTRAINT [CK_Pedido_direccion] CHECK (([modalidadEntrega]=N'Retiro' OR [direccionEntrega] IS NOT NULL AND len(Trim([direccionEntrega]))>(0))),
        CONSTRAINT [CK_Pedido_entrega] CHECK (([estado]=N'Entregado' AND [fechaEntregaReal] IS NOT NULL OR [estado]<>N'Entregado' AND [fechaEntregaReal] IS NULL)),
        CONSTRAINT [CK_Pedido_estado] CHECK (([estado]=N'Entregado' OR [estado]=N'Listo para entrega' OR [estado]=N'En producción' OR [estado]=N'Pendiente')),
        CONSTRAINT [CK_Pedido_modalidad] CHECK (([modalidadEntrega]=N'Domicilio' OR [modalidadEntrega]=N'Retiro')),
        CONSTRAINT [FK_Pedido_Usuario_cliente] FOREIGN KEY ([cliente]) REFERENCES [Usuario] ([id]),
        CONSTRAINT [FK_Pedido_Usuario_responsableArchivo] FOREIGN KEY ([responsableArchivo]) REFERENCES [Usuario] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE TABLE [VersionCotizacion] (
        [id] int NOT NULL IDENTITY,
        [numeroVersion] int NOT NULL,
        [pedido] int NOT NULL,
        [fechaRegistro] datetimeoffset(0) NOT NULL,
        [responsableRegistro] int NOT NULL,
        [decision] nvarchar(10) NOT NULL CONSTRAINT [DF_VersionCotizacion_decision] DEFAULT N'Propuesta',
        [fechaDecision] datetimeoffset(0) NULL,
        [responsableDecision] int NULL,
        [referenciaDecision] nvarchar(250) NULL,
        CONSTRAINT [PK_VersionCotizacion] PRIMARY KEY ([id]),
        CONSTRAINT [CK_VersionCotizacion_decision] CHECK (([decision]=N'Propuesta' AND [fechaDecision] IS NULL AND [responsableDecision] IS NULL AND [referenciaDecision] IS NULL OR ([decision]=N'Rechazada' OR [decision]=N'Aceptada') AND [fechaDecision] IS NOT NULL AND [fechaDecision]>=[fechaRegistro] AND [responsableDecision] IS NOT NULL AND [referenciaDecision] IS NOT NULL AND len(Trim([referenciaDecision]))>(0))),
        CONSTRAINT [CK_VersionCotizacion_numero] CHECK (([numeroVersion]>(0))),
        CONSTRAINT [FK_VersionCotizacion_Pedido_pedido] FOREIGN KEY ([pedido]) REFERENCES [Pedido] ([id]),
        CONSTRAINT [FK_VersionCotizacion_Usuario_responsableDecision] FOREIGN KEY ([responsableDecision]) REFERENCES [Usuario] ([id]),
        CONSTRAINT [FK_VersionCotizacion_Usuario_responsableRegistro] FOREIGN KEY ([responsableRegistro]) REFERENCES [Usuario] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_ComprobacionSublimacion_responsable] ON [ComprobacionSublimacion] ([responsable]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_ComprobacionSublimacion_versionCotizacion] ON [ComprobacionSublimacion] ([versionCotizacion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_ConstanciaEntrega_pedido] ON [ConstanciaEntrega] ([pedido]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_ConstanciaEntrega_responsable] ON [ConstanciaEntrega] ([responsable]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_DetalleCotizacion_detalle] ON [DetalleCotizacion] ([detalle]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_DetalleCotizacion_versionDiseno] ON [DetalleCotizacion] ([versionDiseno]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_DetallePedido_pedido] ON [DetallePedido] ([pedido]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_DetallePedido_producto] ON [DetallePedido] ([producto]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_DetallePedido_versionDisenoVigente] ON [DetallePedido] ([versionDisenoVigente]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_DisenoComprobado_versionDiseno] ON [DisenoComprobado] ([versionDiseno]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pago_pedido_estado] ON [Pago] ([pedido], [estadoVerificacion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pago_pedido_estadoVerificacion] ON [Pago] ([pedido], [estadoVerificacion]) INCLUDE ([monto]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pago_responsableVerificacion] ON [Pago] ([responsableVerificacion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Pago_confirmacion] ON [Pago] ([pedido], [confirmacion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Pago_referenciaVerificada] ON [Pago] ([pedido], [medio], [referencia]) WHERE ([estadoVerificacion]=N''Verificado'')');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedido_cliente_fecha] ON [Pedido] ([cliente], [fechaOrden]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedido_responsableArchivo] ON [Pedido] ([responsableArchivo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedido_versionCotizacionVigente] ON [Pedido] ([versionCotizacionVigente]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Pedido_codigo] ON [Pedido] ([codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Pedido_confirmacion] ON [Pedido] ([cliente], [confirmacion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Usuario_correo] ON [Usuario] ([correo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_VersionCotizacion_responsableDecision] ON [VersionCotizacion] ([responsableDecision]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_VersionCotizacion_responsableRegistro] ON [VersionCotizacion] ([responsableRegistro]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_VersionCotizacion_numero] ON [VersionCotizacion] ([pedido], [numeroVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_VersionDiseno_responsableDecision] ON [VersionDiseno] ([responsableDecision]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE INDEX [IX_VersionDiseno_responsableRegistro] ON [VersionDiseno] ([responsableRegistro]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_VersionDiseno_numero] ON [VersionDiseno] ([detalle], [numeroVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [ComprobacionSublimacion] ADD CONSTRAINT [FK_ComprobacionSublimacion_VersionCotizacion_versionCotizacion] FOREIGN KEY ([versionCotizacion]) REFERENCES [VersionCotizacion] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [ConstanciaEntrega] ADD CONSTRAINT [FK_ConstanciaEntrega_Pedido_pedido] FOREIGN KEY ([pedido]) REFERENCES [Pedido] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [DetalleCotizacion] ADD CONSTRAINT [FK_DetalleCotizacion_DetallePedido_detalle] FOREIGN KEY ([detalle]) REFERENCES [DetallePedido] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [DetalleCotizacion] ADD CONSTRAINT [FK_DetalleCotizacion_VersionCotizacion_versionCotizacion] FOREIGN KEY ([versionCotizacion]) REFERENCES [VersionCotizacion] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [DetalleCotizacion] ADD CONSTRAINT [FK_DetalleCotizacion_VersionDiseno_versionDiseno] FOREIGN KEY ([versionDiseno]) REFERENCES [VersionDiseno] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [DetallePedido] ADD CONSTRAINT [FK_DetallePedido_Pedido_pedido] FOREIGN KEY ([pedido]) REFERENCES [Pedido] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [DetallePedido] ADD CONSTRAINT [FK_Detalle_versionVigente] FOREIGN KEY ([versionDisenoVigente]) REFERENCES [VersionDiseno] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [Pago] ADD CONSTRAINT [FK_Pago_Pedido_pedido] FOREIGN KEY ([pedido]) REFERENCES [Pedido] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    ALTER TABLE [Pedido] ADD CONSTRAINT [FK_Pedido_versionVigente] FOREIGN KEY ([versionCotizacionVigente]) REFERENCES [VersionCotizacion] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE VIEW dbo.vDetalleCotizacion AS
    SELECT c.*, d.cantidad,
           CAST(d.cantidad * c.precioUnitarioAcordado AS decimal(28,2)) AS subtotal
    FROM dbo.DetalleCotizacion c JOIN dbo.DetallePedido d ON d.id = c.detalle;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE VIEW dbo.vVersionCotizacion AS
    SELECT v.*, t.total
    FROM dbo.VersionCotizacion v
    OUTER APPLY (SELECT CAST(SUM(d.subtotal) AS decimal(28,2)) AS total
                 FROM dbo.vDetalleCotizacion d WHERE d.versionCotizacion = v.id) t;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE VIEW dbo.vPedido AS
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
    OUTER APPLY (SELECT SUM(g.monto) AS verificado FROM dbo.Pago g WHERE g.pedido = p.id AND g.estadoVerificacion = N''Verificado'') a;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE VIEW dbo.vDetallePedido AS
    SELECT d.*, c.precioUnitarioAcordado,
           CAST(c.cantidad * c.precioUnitarioAcordado AS decimal(28,2)) AS subtotal,
           CAST(CASE WHEN v.id IS NULL THEN 0 ELSE 1 END AS bit) AS disenoAprobado,
           v.fechaDecision AS fechaAprobacionDiseno,
           v.responsableDecision AS responsableAprobacionDiseno
    FROM dbo.DetallePedido d
    JOIN dbo.Pedido p ON p.id = d.pedido
    LEFT JOIN dbo.vDetalleCotizacion c ON c.versionCotizacion = p.versionCotizacionVigente AND c.detalle = d.id
    LEFT JOIN dbo.VersionDiseno v ON v.id = d.versionDisenoVigente;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE VIEW dbo.Constancia AS
    SELECT CAST(N''Cotización'' AS nvarchar(15)) AS tipo, v.id AS registroOrigen, v.pedido,
           CAST(CASE WHEN v.decision = N''Aceptada'' THEN 1 ELSE 0 END AS bit) AS decision,
           v.fechaDecision AS fecha, v.responsableDecision AS responsable,
           v.referenciaDecision AS referencia, v.id AS versionCotizacion
    FROM dbo.VersionCotizacion v WHERE v.decision IN (N''Aceptada'', N''Rechazada'')
    UNION ALL
    SELECT N''Entrega'', e.id, e.pedido, e.decision, e.fecha, e.responsable, e.referencia, CAST(NULL AS int)
    FROM dbo.ConstanciaEntrega e;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_Pedido_versionPropia ON dbo.Pedido AFTER INSERT, UPDATE AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM inserted p JOIN dbo.VersionCotizacion v ON v.id = p.versionCotizacionVigente
                   WHERE v.pedido <> p.id OR v.decision <> N''Aceptada'')
            THROW 51001, N''La cotización vigente debe pertenecer al pedido y estar aceptada.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_Detalle_versionPropia ON dbo.DetallePedido AFTER INSERT, UPDATE AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM inserted d JOIN dbo.VersionDiseno v ON v.id = d.versionDisenoVigente
                   WHERE v.detalle <> d.id OR v.decision <> N''Aprobada'')
            THROW 51002, N''El diseño vigente debe pertenecer al detalle y estar aprobado.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_DetalleCotizacion_pertenencia ON dbo.DetalleCotizacion AFTER INSERT AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.VersionCotizacion v ON v.id = i.versionCotizacion
                   JOIN dbo.DetallePedido d ON d.id = i.detalle LEFT JOIN dbo.VersionDiseno a ON a.id = i.versionDiseno
                   WHERE v.pedido <> d.pedido OR v.decision <> N''Propuesta'' OR (a.id IS NOT NULL AND a.detalle <> d.id))
            THROW 51003, N''Los importes y diseños deben corresponder al pedido de una cotización propuesta.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_DetalleCotizacion_inmutable ON dbo.DetalleCotizacion AFTER UPDATE, DELETE AS
    BEGIN
        THROW 51004, N''Los importes históricos no se sobrescriben; crear otra versión.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_VersionDiseno_inmutable ON dbo.VersionDiseno AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.decision <> N''Pendiente'')
            THROW 51005, N''No se elimina ni se vuelve a decidir una versión de diseño.'', 1;
        IF EXISTS (SELECT id, numeroVersion, detalle, archivoURL, descripcion, fechaRegistro, responsableRegistro FROM deleted
                   EXCEPT SELECT id, numeroVersion, detalle, archivoURL, descripcion, fechaRegistro, responsableRegistro FROM inserted)
            THROW 51006, N''Una revisión del contenido requiere otra versión de diseño.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_VersionCotizacion_inmutable ON dbo.VersionCotizacion AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.decision <> N''Propuesta'')
            THROW 51007, N''No se elimina ni se vuelve a decidir una versión de cotización.'', 1;
        IF EXISTS (SELECT id, numeroVersion, pedido, fechaRegistro, responsableRegistro FROM deleted
                   EXCEPT SELECT id, numeroVersion, pedido, fechaRegistro, responsableRegistro FROM inserted)
            THROW 51008, N''Una revisión requiere otra versión de cotización.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_DisenoComprobado_pertenencia ON dbo.DisenoComprobado AFTER INSERT AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ComprobacionSublimacion c ON c.id = i.comprobacion
                   JOIN dbo.VersionCotizacion q ON q.id = c.versionCotizacion
                   JOIN dbo.VersionDiseno v ON v.id = i.versionDiseno JOIN dbo.DetallePedido d ON d.id = v.detalle
                   WHERE d.pedido <> q.pedido OR v.decision <> N''Aprobada'' OR q.decision <> N''Aceptada'')
            THROW 51009, N''La comprobación requiere versiones aprobadas del mismo pedido.'', 1;
        IF EXISTS (SELECT c.comprobacion, v.detalle FROM dbo.DisenoComprobado c JOIN dbo.VersionDiseno v ON v.id = c.versionDiseno
                   WHERE c.comprobacion IN (SELECT comprobacion FROM inserted) GROUP BY c.comprobacion, v.detalle HAVING COUNT(*) > 1)
            THROW 51010, N''Una comprobación registra una sola versión por detalle.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_Pago_decisionUnica ON dbo.Pago AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.estadoVerificacion <> N''Pendiente'')
            THROW 51011, N''Un pago resuelto conserva su decisión e importe; no se elimina ni se vuelve a acreditar.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_Detalle_solicitudHistorica ON dbo.DetallePedido AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (SELECT id,pedido,producto,cantidad,talla,color,tela,empaque,observaciones,ideaDiseno,archivoDisenoURL,precioBaseReferencia FROM deleted
                   EXCEPT SELECT id,pedido,producto,cantidad,talla,color,tela,empaque,observaciones,ideaDiseno,archivoDisenoURL,precioBaseReferencia FROM inserted)
            THROW 51015, N''La solicitud original se conserva; los cambios de diseño se registran en sus versiones.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_ConstanciaEntrega_inmutable ON dbo.ConstanciaEntrega AFTER UPDATE, DELETE AS
    BEGIN
        THROW 51012, N''Las constancias conservan su contenido; una nueva decisión requiere otro registro.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_Comprobacion_inmutable ON dbo.ComprobacionSublimacion AFTER UPDATE, DELETE AS
    BEGIN
        THROW 51013, N''Las comprobaciones conservan el contexto histórico de versiones y pagos.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    EXEC(N'CREATE TRIGGER dbo.TR_DisenoComprobado_inmutable ON dbo.DisenoComprobado AFTER UPDATE, DELETE AS
    BEGIN
        THROW 51014, N''Los diseños de una comprobación histórica no se sobrescriben.'', 1;
    END;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028165048_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028165048_Inicial', N'10.0.14');
END;

COMMIT;
GO
