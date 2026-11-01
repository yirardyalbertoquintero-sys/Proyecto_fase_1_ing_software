using ImpactoTextil.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Infrastructure;

public partial class ImpactoTextilDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Restricciones recuperadas del prototipo probado; las migraciones son la autoridad.
        modelBuilder.Entity<ComprobacionSublimacion>().ToTable("ComprobacionSublimacion", tabla =>
        {
            tabla.HasCheckConstraint("CK_Comprobacion_monto", "([montoVerificado]>=(0))");
        });
        modelBuilder.Entity<ConstanciaEntrega>().ToTable("ConstanciaEntrega", tabla =>
        {
            tabla.HasCheckConstraint("CK_ConstanciaEntrega_referencia", "(len(Trim([referencia]))>(0))");
        });
        modelBuilder.Entity<DetalleCotizacion>().ToTable("DetalleCotizacion", tabla =>
        {
            tabla.HasCheckConstraint("CK_DetalleCotizacion_importes", "([precioUnitarioAcordado]>(0))");
        });
        modelBuilder.Entity<DetallePedido>().ToTable("DetallePedido", tabla =>
        {
            tabla.HasCheckConstraint("CK_Detalle_cantidad", "([cantidad]>(0))");
            tabla.HasCheckConstraint("CK_Detalle_precioReferencia", "([precioBaseReferencia]>(0))");
            tabla.HasCheckConstraint("CK_Detalle_textos", "(len(Trim([talla]))>(0) AND len(Trim([color]))>(0) AND len(Trim([tela]))>(0) AND len(Trim([empaque]))>(0))");
            tabla.HasCheckConstraint("CK_Detalle_diseno", "([archivoDisenoURL] IS NOT NULL AND len(Trim([archivoDisenoURL]))>(0) OR [ideaDiseno] IS NOT NULL AND len(Trim([ideaDiseno]))>(0))");
        });
        modelBuilder.Entity<Pago>().ToTable("Pago", tabla =>
        {
            tabla.HasCheckConstraint("CK_Pago_monto", "([monto]>(0))");
            tabla.HasCheckConstraint("CK_Pago_huellaComprobante", "([huellaComprobante] IS NULL OR DATALENGTH([huellaComprobante]) = 32)");
            tabla.HasCheckConstraint("CK_Pago_medio", "([medio]=N'Tarjeta' OR [medio]=N'Depósito' OR [medio]=N'Transferencia' OR [medio]=N'Efectivo')");
            tabla.HasCheckConstraint("CK_Pago_resolucion", "([estadoVerificacion]=N'Pendiente' AND [fechaVerificacion] IS NULL AND [responsableVerificacion] IS NULL AND [comprobanteURL] IS NOT NULL AND len(Trim([comprobanteURL]))>(0) OR ([estadoVerificacion]=N'Rechazado' OR [estadoVerificacion]=N'Verificado') AND [fechaVerificacion] IS NOT NULL AND [responsableVerificacion] IS NOT NULL)");
            tabla.HasCheckConstraint("CK_Pago_referencia", "([estadoVerificacion]<>N'Verificado' OR [referencia] IS NOT NULL AND len(Trim([referencia]))>(0))");
            tabla.HasCheckConstraint("CK_Pago_respaldo", "([comprobanteURL] IS NOT NULL AND len(Trim([comprobanteURL]))>(0) OR [estadoVerificacion]=N'Verificado' AND [referencia] IS NOT NULL AND len(Trim([referencia]))>(0))");
        });
        modelBuilder.Entity<Pedido>().ToTable("Pedido", tabla =>
        {
            tabla.HasCheckConstraint("CK_Pedido_codigo", "(len(Trim([codigo]))>(0))");
            tabla.HasCheckConstraint("CK_Pedido_estado", "([estado]=N'Entregado' OR [estado]=N'Listo para entrega' OR [estado]=N'En producción' OR [estado]=N'Pendiente')");
            tabla.HasCheckConstraint("CK_Pedido_modalidad", "([modalidadEntrega]=N'Domicilio' OR [modalidadEntrega]=N'Retiro')");
            tabla.HasCheckConstraint("CK_Pedido_direccion", "([modalidadEntrega]=N'Retiro' OR [direccionEntrega] IS NOT NULL AND len(Trim([direccionEntrega]))>(0))");
            tabla.HasCheckConstraint("CK_Pedido_entrega", "([estado]=N'Entregado' AND [fechaEntregaReal] IS NOT NULL OR [estado]<>N'Entregado' AND [fechaEntregaReal] IS NULL)");
            tabla.HasCheckConstraint("CK_Pedido_archivo", "([condicionAdministrativa]=N'Activa' AND [motivoArchivo] IS NULL AND [fechaArchivo] IS NULL AND [responsableArchivo] IS NULL OR [condicionAdministrativa]=N'Archivada' AND [estado]=N'Pendiente' AND [versionCotizacionVigente] IS NULL AND [motivoArchivo] IS NOT NULL AND len(Trim([motivoArchivo]))>(0) AND [fechaArchivo] IS NOT NULL AND [responsableArchivo] IS NOT NULL)");
        });
        modelBuilder.Entity<Producto>().ToTable("Producto", tabla =>
        {
            tabla.HasCheckConstraint("CK_Producto_textos", "(len(Trim([nombre]))>(0) AND len(Trim([color]))>(0) AND len(Trim([talla]))>(0) AND len(Trim([tela]))>(0) AND len(Trim([imagenURL]))>(0))");
            tabla.HasCheckConstraint("CK_Producto_precio", "([precioBase]>(0))");
        });
        modelBuilder.Entity<Usuario>().ToTable("Usuario", tabla =>
        {
            tabla.HasCheckConstraint("CK_Usuario_nombre", "(len(Trim([nombre]))>(0))");
            tabla.HasCheckConstraint("CK_Usuario_correo", "(len(Trim([correo]))>(0) AND datalength([correo])=datalength(Trim([correo])))");
            tabla.HasCheckConstraint("CK_Usuario_rol", "([rol]=N'Administrador' OR [rol]=N'Cliente')");
            tabla.HasCheckConstraint("CK_Usuario_hash", "(len([passwordHash])=(60) AND (left([passwordHash],(4))='$2y$' OR left([passwordHash],(4))='$2b$' OR left([passwordHash],(4))='$2a$'))");
            tabla.HasCheckConstraint("CK_Usuario_consentimiento", "([aceptacionTerminos]=(1) AND [fechaAceptacion] IS NOT NULL OR [aceptacionTerminos]=(0) AND [fechaAceptacion] IS NULL AND [rol]=N'Administrador')");
        });
        modelBuilder.Entity<VersionCotizacion>().ToTable("VersionCotizacion", tabla =>
        {
            tabla.HasCheckConstraint("CK_VersionCotizacion_numero", "([numeroVersion]>(0))");
            tabla.HasCheckConstraint("CK_VersionCotizacion_decision", "([decision]=N'Propuesta' AND [fechaDecision] IS NULL AND [responsableDecision] IS NULL AND [referenciaDecision] IS NULL OR ([decision]=N'Rechazada' OR [decision]=N'Aceptada') AND [fechaDecision] IS NOT NULL AND [fechaDecision]>=[fechaRegistro] AND [responsableDecision] IS NOT NULL AND [referenciaDecision] IS NOT NULL AND len(Trim([referenciaDecision]))>(0))");
        });
        modelBuilder.Entity<VersionDiseno>().ToTable("VersionDiseno", tabla =>
        {
            tabla.HasCheckConstraint("CK_VersionDiseno_numero", "([numeroVersion]>(0))");
            tabla.HasCheckConstraint("CK_VersionDiseno_contenido", "([archivoURL] IS NOT NULL AND len(Trim([archivoURL]))>(0) OR [descripcion] IS NOT NULL AND len(Trim([descripcion]))>(0))");
            tabla.HasCheckConstraint("CK_VersionDiseno_decision", "([decision]=N'Pendiente' AND [fechaDecision] IS NULL AND [responsableDecision] IS NULL AND [referenciaDecision] IS NULL OR ([decision]=N'Rechazada' OR [decision]=N'Aprobada') AND [fechaDecision] IS NOT NULL AND [fechaDecision]>=[fechaRegistro] AND [responsableDecision] IS NOT NULL AND [referenciaDecision] IS NOT NULL AND len(Trim([referenciaDecision]))>(0))");
        });
        modelBuilder.Entity<Pago>().HasIndex(p => new { p.pedido, p.estadoVerificacion })
            .IncludeProperties(p => p.monto);
        // SQL Server exige evitar OUTPUT directo cuando existen disparadores.
        foreach (var entidad in modelBuilder.Model.GetEntityTypes().Where(t => t.GetViewName() is null))
            entidad.UseSqlOutputClause(false);
    }
}
