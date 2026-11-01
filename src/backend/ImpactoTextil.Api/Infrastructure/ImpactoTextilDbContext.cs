using System;
using System.Collections.Generic;
using ImpactoTextil.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImpactoTextil.Api.Infrastructure;

public partial class ImpactoTextilDbContext : DbContext
{
    public ImpactoTextilDbContext(DbContextOptions<ImpactoTextilDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ComprobacionSublimacion> ComprobacionSublimacion { get; set; }

    public virtual DbSet<Constancia> Constancia { get; set; }

    public virtual DbSet<ConstanciaEntrega> ConstanciaEntrega { get; set; }

    public virtual DbSet<DetalleCotizacion> DetalleCotizacion { get; set; }

    public virtual DbSet<DetallePedido> DetallePedido { get; set; }

    public virtual DbSet<Pago> Pago { get; set; }

    public virtual DbSet<Pedido> Pedido { get; set; }

    public virtual DbSet<Producto> Producto { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    public virtual DbSet<VersionCotizacion> VersionCotizacion { get; set; }

    public virtual DbSet<VersionDiseno> VersionDiseno { get; set; }

    public virtual DbSet<vDetalleCotizacion> vDetalleCotizacion { get; set; }

    public virtual DbSet<vDetallePedido> vDetallePedido { get; set; }

    public virtual DbSet<vPedido> vPedido { get; set; }

    public virtual DbSet<vVersionCotizacion> vVersionCotizacion { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComprobacionSublimacion>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.ToTable(tb => tb.HasTrigger("TR_Comprobacion_inmutable"));

            entity.Property(e => e.fecha).HasPrecision(0);
            entity.Property(e => e.montoVerificado).HasColumnType("decimal(28, 2)");

            entity.HasOne(d => d.responsableNavigation).WithMany(p => p.ComprobacionSublimacion)
                .HasForeignKey(d => d.responsable)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.versionCotizacionNavigation).WithMany(p => p.ComprobacionSublimacion)
                .HasForeignKey(d => d.versionCotizacion)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasMany(d => d.versionDiseno).WithMany(p => p.comprobacion)
                .UsingEntity<DisenoComprobado>(
                    "DisenoComprobado",
                    r => r.HasOne<VersionDiseno>().WithMany()
                        .HasForeignKey("versionDiseno")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    l => l.HasOne<ComprobacionSublimacion>().WithMany()
                        .HasForeignKey("comprobacion")
                        .OnDelete(DeleteBehavior.ClientSetNull),
                    j =>
                    {
                        j.HasKey("comprobacion", "versionDiseno");
                        j.ToTable(tb =>
                            {
                                tb.HasTrigger("TR_DisenoComprobado_inmutable");
                                tb.HasTrigger("TR_DisenoComprobado_pertenencia");
                            });
                    });
        });

        modelBuilder.Entity<Constancia>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Constancia");

            entity.Property(e => e.fecha).HasPrecision(0);
            entity.Property(e => e.referencia).HasMaxLength(250);
            entity.Property(e => e.tipo).HasMaxLength(15);
        });

        modelBuilder.Entity<ConstanciaEntrega>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.ToTable(tb => tb.HasTrigger("TR_ConstanciaEntrega_inmutable"));

            entity.Property(e => e.fecha).HasPrecision(0);
            entity.Property(e => e.referencia).HasMaxLength(250);

            entity.HasOne(d => d.pedidoNavigation).WithMany(p => p.ConstanciaEntrega)
                .HasForeignKey(d => d.pedido)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.responsableNavigation).WithMany(p => p.ConstanciaEntrega)
                .HasForeignKey(d => d.responsable)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<DetalleCotizacion>(entity =>
        {
            entity.HasKey(e => new { e.versionCotizacion, e.detalle });

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("TR_DetalleCotizacion_inmutable");
                    tb.HasTrigger("TR_DetalleCotizacion_pertenencia");
                });

            entity.Property(e => e.precioUnitarioAcordado).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.detalleNavigation).WithMany(p => p.DetalleCotizacion)
                .HasForeignKey(d => d.detalle)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.versionCotizacionNavigation).WithMany(p => p.DetalleCotizacion)
                .HasForeignKey(d => d.versionCotizacion)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.versionDisenoNavigation).WithMany(p => p.DetalleCotizacion)
                .HasForeignKey(d => d.versionDiseno);
        });

        modelBuilder.Entity<DetallePedido>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("TR_Detalle_solicitudHistorica");
                    tb.HasTrigger("TR_Detalle_versionPropia");
                });

            entity.HasIndex(e => e.pedido, "IX_DetallePedido_pedido");

            entity.Property(e => e.archivoDisenoURL).HasMaxLength(2048);
            entity.Property(e => e.color).HasMaxLength(50);
            entity.Property(e => e.empaque).HasMaxLength(100);
            entity.Property(e => e.ideaDiseno).HasMaxLength(1000);
            entity.Property(e => e.observaciones).HasMaxLength(1000);
            entity.Property(e => e.precioBaseReferencia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.talla).HasMaxLength(30);
            entity.Property(e => e.tela).HasMaxLength(100);

            entity.HasOne(d => d.pedidoNavigation).WithMany(p => p.DetallePedido)
                .HasForeignKey(d => d.pedido)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.productoNavigation).WithMany(p => p.DetallePedido)
                .HasForeignKey(d => d.producto)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.versionDisenoVigenteNavigation).WithMany(p => p.DetallePedido)
                .HasForeignKey(d => d.versionDisenoVigente)
                .HasConstraintName("FK_Detalle_versionVigente");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.ToTable(tb => tb.HasTrigger("TR_Pago_decisionUnica"));

            entity.HasIndex(e => new { e.pedido, e.estadoVerificacion }, "IX_Pago_pedido_estado");

            entity.HasIndex(e => new { e.pedido, e.confirmacion }, "UQ_Pago_confirmacion").IsUnique();

            entity.HasIndex(e => new { e.pedido, e.medio, e.referencia }, "UX_Pago_referenciaVerificada")
                .IsUnique()
                .HasFilter("([estadoVerificacion]=N'Verificado')");

            entity.Property(e => e.comprobanteURL).HasMaxLength(2048);
            entity.Property(e => e.huellaComprobante).HasMaxLength(32);
            entity.Property(e => e.controlConcurrencia)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.estadoVerificacion)
                .HasMaxLength(10)
                .HasDefaultValue("Pendiente", "DF_Pago_estado");
            entity.Property(e => e.fecha).HasPrecision(0);
            entity.Property(e => e.fechaVerificacion).HasPrecision(0);
            entity.Property(e => e.medio).HasMaxLength(20);
            entity.Property(e => e.monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.referencia).HasMaxLength(100);

            entity.HasOne(d => d.pedidoNavigation).WithMany(p => p.Pago)
                .HasForeignKey(d => d.pedido)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.responsableVerificacionNavigation).WithMany(p => p.Pago)
                .HasForeignKey(d => d.responsableVerificacion);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.ToTable(tb => tb.HasTrigger("TR_Pedido_versionPropia"));

            entity.HasIndex(e => new { e.cliente, e.fechaOrden }, "IX_Pedido_cliente_fecha");

            entity.HasIndex(e => e.codigo, "UQ_Pedido_codigo").IsUnique();

            entity.HasIndex(e => new { e.cliente, e.confirmacion }, "UQ_Pedido_confirmacion").IsUnique();

            entity.Property(e => e.codigo).HasMaxLength(50);
            entity.Property(e => e.condicionAdministrativa)
                .HasMaxLength(10)
                .HasDefaultValue("Activa", "DF_Pedido_condicion");
            entity.Property(e => e.controlConcurrencia)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.direccionEntrega).HasMaxLength(250);
            entity.Property(e => e.estado)
                .HasMaxLength(30)
                .HasDefaultValue("Pendiente", "DF_Pedido_estado");
            entity.Property(e => e.fechaArchivo).HasPrecision(0);
            entity.Property(e => e.fechaEntregaReal).HasPrecision(0);
            entity.Property(e => e.fechaOrden).HasPrecision(0);
            entity.Property(e => e.huellaSolicitud)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.modalidadEntrega).HasMaxLength(15);
            entity.Property(e => e.motivoArchivo).HasMaxLength(250);

            entity.HasOne(d => d.clienteNavigation).WithMany(p => p.PedidoclienteNavigation)
                .HasForeignKey(d => d.cliente)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.responsableArchivoNavigation).WithMany(p => p.PedidoresponsableArchivoNavigation)
                .HasForeignKey(d => d.responsableArchivo);

            entity.HasOne(d => d.versionCotizacionVigenteNavigation).WithMany(p => p.Pedido)
                .HasForeignKey(d => d.versionCotizacionVigente)
                .HasConstraintName("FK_Pedido_versionVigente");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.Property(e => e.activo).HasDefaultValue(true, "DF_Producto_activo");
            entity.Property(e => e.color).HasMaxLength(50);
            entity.Property(e => e.imagenURL).HasMaxLength(2048);
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.precioBase).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.talla).HasMaxLength(30);
            entity.Property(e => e.tela).HasMaxLength(100);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.HasIndex(e => e.correo, "UQ_Usuario_correo").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true, "DF_Usuario_activo");
            entity.Property(e => e.correo)
                .HasMaxLength(150)
                .UseCollation("Latin1_General_100_CI_AS_SC");
            entity.Property(e => e.fechaAceptacion).HasPrecision(0);
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.passwordHash)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.rol).HasMaxLength(20);
        });

        modelBuilder.Entity<VersionCotizacion>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.ToTable(tb => tb.HasTrigger("TR_VersionCotizacion_inmutable"));

            entity.HasIndex(e => new { e.pedido, e.numeroVersion }, "UQ_VersionCotizacion_numero").IsUnique();

            entity.Property(e => e.decision)
                .HasMaxLength(10)
                .HasDefaultValue("Propuesta", "DF_VersionCotizacion_decision");
            entity.Property(e => e.fechaDecision).HasPrecision(0);
            entity.Property(e => e.fechaRegistro).HasPrecision(0);
            entity.Property(e => e.referenciaDecision).HasMaxLength(250);

            entity.HasOne(d => d.pedidoNavigation).WithMany(p => p.VersionCotizacion)
                .HasForeignKey(d => d.pedido)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.responsableDecisionNavigation).WithMany(p => p.VersionCotizacionresponsableDecisionNavigation)
                .HasForeignKey(d => d.responsableDecision);

            entity.HasOne(d => d.responsableRegistroNavigation).WithMany(p => p.VersionCotizacionresponsableRegistroNavigation)
                .HasForeignKey(d => d.responsableRegistro)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VersionDiseno>(entity =>
        {
            entity.HasKey(e => e.id);

            entity.ToTable(tb => tb.HasTrigger("TR_VersionDiseno_inmutable"));

            entity.HasIndex(e => new { e.detalle, e.numeroVersion }, "UQ_VersionDiseno_numero").IsUnique();

            entity.Property(e => e.archivoURL).HasMaxLength(2048);
            entity.Property(e => e.decision)
                .HasMaxLength(10)
                .HasDefaultValue("Pendiente", "DF_VersionDiseno_decision");
            entity.Property(e => e.descripcion).HasMaxLength(1000);
            entity.Property(e => e.fechaDecision).HasPrecision(0);
            entity.Property(e => e.fechaRegistro).HasPrecision(0);
            entity.Property(e => e.referenciaDecision).HasMaxLength(250);

            entity.HasOne(d => d.detalleNavigation).WithMany(p => p.VersionDiseno)
                .HasForeignKey(d => d.detalle)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.responsableDecisionNavigation).WithMany(p => p.VersionDisenoresponsableDecisionNavigation)
                .HasForeignKey(d => d.responsableDecision);

            entity.HasOne(d => d.responsableRegistroNavigation).WithMany(p => p.VersionDisenoresponsableRegistroNavigation)
                .HasForeignKey(d => d.responsableRegistro)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<vDetalleCotizacion>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vDetalleCotizacion");

            entity.Property(e => e.precioUnitarioAcordado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.subtotal).HasColumnType("decimal(28, 2)");
        });

        modelBuilder.Entity<vDetallePedido>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vDetallePedido");

            entity.Property(e => e.archivoDisenoURL).HasMaxLength(2048);
            entity.Property(e => e.color).HasMaxLength(50);
            entity.Property(e => e.empaque).HasMaxLength(100);
            entity.Property(e => e.fechaAprobacionDiseno).HasPrecision(0);
            entity.Property(e => e.ideaDiseno).HasMaxLength(1000);
            entity.Property(e => e.observaciones).HasMaxLength(1000);
            entity.Property(e => e.precioBaseReferencia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.precioUnitarioAcordado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.subtotal).HasColumnType("decimal(28, 2)");
            entity.Property(e => e.talla).HasMaxLength(30);
            entity.Property(e => e.tela).HasMaxLength(100);
        });

        modelBuilder.Entity<vPedido>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vPedido");

            entity.Property(e => e.anticipoExigible).HasColumnType("decimal(28, 2)");
            entity.Property(e => e.codigo).HasMaxLength(50);
            entity.Property(e => e.condicionAdministrativa).HasMaxLength(10);
            entity.Property(e => e.controlConcurrencia)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.direccionEntrega).HasMaxLength(250);
            entity.Property(e => e.estado).HasMaxLength(30);
            entity.Property(e => e.fechaArchivo).HasPrecision(0);
            entity.Property(e => e.fechaEntregaReal).HasPrecision(0);
            entity.Property(e => e.fechaOrden).HasPrecision(0);
            entity.Property(e => e.huellaSolicitud)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.modalidadEntrega).HasMaxLength(15);
            entity.Property(e => e.motivoArchivo).HasMaxLength(250);
            entity.Property(e => e.porcentajeAnticipo).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.saldoPendiente).HasColumnType("decimal(28, 2)");
            entity.Property(e => e.total).HasColumnType("decimal(28, 2)");
            entity.Property(e => e.totalReferencial).HasColumnType("decimal(28, 2)");
        });

        modelBuilder.Entity<vVersionCotizacion>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vVersionCotizacion");

            entity.Property(e => e.decision).HasMaxLength(10);
            entity.Property(e => e.fechaDecision).HasPrecision(0);
            entity.Property(e => e.fechaRegistro).HasPrecision(0);
            entity.Property(e => e.referenciaDecision).HasMaxLength(250);
            entity.Property(e => e.total).HasColumnType("decimal(28, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
