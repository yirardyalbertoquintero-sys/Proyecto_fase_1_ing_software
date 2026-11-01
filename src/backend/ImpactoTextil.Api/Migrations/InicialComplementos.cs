using Microsoft.EntityFrameworkCore.Migrations;

namespace ImpactoTextil.Api.Migrations;

// Definiciones congeladas de la migración inicial. Los cambios futuros requieren otra migración.
internal static class InicialComplementos
{
    internal static void Crear(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;");
        migrationBuilder.Sql("""
            EXEC(N'CREATE VIEW dbo.vDetalleCotizacion AS
            SELECT c.*, d.cantidad,
                   CAST(d.cantidad * c.precioUnitarioAcordado AS decimal(28,2)) AS subtotal
            FROM dbo.DetalleCotizacion c JOIN dbo.DetallePedido d ON d.id = c.detalle;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE VIEW dbo.vVersionCotizacion AS
            SELECT v.*, t.total
            FROM dbo.VersionCotizacion v
            OUTER APPLY (SELECT CAST(SUM(d.subtotal) AS decimal(28,2)) AS total
                         FROM dbo.vDetalleCotizacion d WHERE d.versionCotizacion = v.id) t;');
            """);
        migrationBuilder.Sql("""
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
            """);
        migrationBuilder.Sql("""
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
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE VIEW dbo.Constancia AS
            SELECT CAST(N''Cotización'' AS nvarchar(15)) AS tipo, v.id AS registroOrigen, v.pedido,
                   CAST(CASE WHEN v.decision = N''Aceptada'' THEN 1 ELSE 0 END AS bit) AS decision,
                   v.fechaDecision AS fecha, v.responsableDecision AS responsable,
                   v.referenciaDecision AS referencia, v.id AS versionCotizacion
            FROM dbo.VersionCotizacion v WHERE v.decision IN (N''Aceptada'', N''Rechazada'')
            UNION ALL
            SELECT N''Entrega'', e.id, e.pedido, e.decision, e.fecha, e.responsable, e.referencia, CAST(NULL AS int)
            FROM dbo.ConstanciaEntrega e;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_Pedido_versionPropia ON dbo.Pedido AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted p JOIN dbo.VersionCotizacion v ON v.id = p.versionCotizacionVigente
                           WHERE v.pedido <> p.id OR v.decision <> N''Aceptada'')
                    THROW 51001, N''La cotización vigente debe pertenecer al pedido y estar aceptada.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_Detalle_versionPropia ON dbo.DetallePedido AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted d JOIN dbo.VersionDiseno v ON v.id = d.versionDisenoVigente
                           WHERE v.detalle <> d.id OR v.decision <> N''Aprobada'')
                    THROW 51002, N''El diseño vigente debe pertenecer al detalle y estar aprobado.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_DetalleCotizacion_pertenencia ON dbo.DetalleCotizacion AFTER INSERT AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.VersionCotizacion v ON v.id = i.versionCotizacion
                           JOIN dbo.DetallePedido d ON d.id = i.detalle LEFT JOIN dbo.VersionDiseno a ON a.id = i.versionDiseno
                           WHERE v.pedido <> d.pedido OR v.decision <> N''Propuesta'' OR (a.id IS NOT NULL AND a.detalle <> d.id))
                    THROW 51003, N''Los importes y diseños deben corresponder al pedido de una cotización propuesta.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_DetalleCotizacion_inmutable ON dbo.DetalleCotizacion AFTER UPDATE, DELETE AS
            BEGIN
                THROW 51004, N''Los importes históricos no se sobrescriben; crear otra versión.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_VersionDiseno_inmutable ON dbo.VersionDiseno AFTER UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.decision <> N''Pendiente'')
                    THROW 51005, N''No se elimina ni se vuelve a decidir una versión de diseño.'', 1;
                IF EXISTS (SELECT id, numeroVersion, detalle, archivoURL, descripcion, fechaRegistro, responsableRegistro FROM deleted
                           EXCEPT SELECT id, numeroVersion, detalle, archivoURL, descripcion, fechaRegistro, responsableRegistro FROM inserted)
                    THROW 51006, N''Una revisión del contenido requiere otra versión de diseño.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_VersionCotizacion_inmutable ON dbo.VersionCotizacion AFTER UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.decision <> N''Propuesta'')
                    THROW 51007, N''No se elimina ni se vuelve a decidir una versión de cotización.'', 1;
                IF EXISTS (SELECT id, numeroVersion, pedido, fechaRegistro, responsableRegistro FROM deleted
                           EXCEPT SELECT id, numeroVersion, pedido, fechaRegistro, responsableRegistro FROM inserted)
                    THROW 51008, N''Una revisión requiere otra versión de cotización.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
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
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_Pago_decisionUnica ON dbo.Pago AFTER UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.id = d.id WHERE i.id IS NULL OR d.estadoVerificacion <> N''Pendiente'')
                    THROW 51011, N''Un pago resuelto conserva su decisión e importe; no se elimina ni se vuelve a acreditar.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_Detalle_solicitudHistorica ON dbo.DetallePedido AFTER UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT id,pedido,producto,cantidad,talla,color,tela,empaque,observaciones,ideaDiseno,archivoDisenoURL,precioBaseReferencia FROM deleted
                           EXCEPT SELECT id,pedido,producto,cantidad,talla,color,tela,empaque,observaciones,ideaDiseno,archivoDisenoURL,precioBaseReferencia FROM inserted)
                    THROW 51015, N''La solicitud original se conserva; los cambios de diseño se registran en sus versiones.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_ConstanciaEntrega_inmutable ON dbo.ConstanciaEntrega AFTER UPDATE, DELETE AS
            BEGIN
                THROW 51012, N''Las constancias conservan su contenido; una nueva decisión requiere otro registro.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_Comprobacion_inmutable ON dbo.ComprobacionSublimacion AFTER UPDATE, DELETE AS
            BEGIN
                THROW 51013, N''Las comprobaciones conservan el contexto histórico de versiones y pagos.'', 1;
            END;');
            """);
        migrationBuilder.Sql("""
            EXEC(N'CREATE TRIGGER dbo.TR_DisenoComprobado_inmutable ON dbo.DisenoComprobado AFTER UPDATE, DELETE AS
            BEGIN
                THROW 51014, N''Los diseños de una comprobación histórica no se sobrescriben.'', 1;
            END;');
            """);
    }

    internal static void EliminarVistas(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW dbo.Constancia;");
        migrationBuilder.Sql("DROP VIEW dbo.vDetallePedido;");
        migrationBuilder.Sql("DROP VIEW dbo.vPedido;");
        migrationBuilder.Sql("DROP VIEW dbo.vVersionCotizacion;");
        migrationBuilder.Sql("DROP VIEW dbo.vDetalleCotizacion;");
    }
}
