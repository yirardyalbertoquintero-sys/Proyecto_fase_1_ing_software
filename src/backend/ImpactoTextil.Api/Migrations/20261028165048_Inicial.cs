using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactoTextil.Api.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    talla = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tela = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    imagenURL = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    precioBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Producto_activo")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.id);
                    table.CheckConstraint("CK_Producto_precio", "([precioBase]>(0))");
                    table.CheckConstraint("CK_Producto_textos", "(len(Trim([nombre]))>(0) AND len(Trim([color]))>(0) AND len(Trim([talla]))>(0) AND len(Trim([tela]))>(0) AND len(Trim([imagenURL]))>(0))");
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, collation: "Latin1_General_100_CI_AS_SC"),
                    passwordHash = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Usuario_activo"),
                    aceptacionTerminos = table.Column<bool>(type: "bit", nullable: false),
                    fechaAceptacion = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.id);
                    table.CheckConstraint("CK_Usuario_consentimiento", "([aceptacionTerminos]=(1) AND [fechaAceptacion] IS NOT NULL OR [aceptacionTerminos]=(0) AND [fechaAceptacion] IS NULL AND [rol]=N'Administrador')");
                    table.CheckConstraint("CK_Usuario_correo", "(len(Trim([correo]))>(0) AND datalength([correo])=datalength(Trim([correo])))");
                    table.CheckConstraint("CK_Usuario_hash", "(len([passwordHash])=(60) AND (left([passwordHash],(4))='$2y$' OR left([passwordHash],(4))='$2b$' OR left([passwordHash],(4))='$2a$'))");
                    table.CheckConstraint("CK_Usuario_nombre", "(len(Trim([nombre]))>(0))");
                    table.CheckConstraint("CK_Usuario_rol", "([rol]=N'Administrador' OR [rol]=N'Cliente')");
                });

            migrationBuilder.CreateTable(
                name: "ComprobacionSublimacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    versionCotizacion = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    responsable = table.Column<int>(type: "int", nullable: false),
                    montoVerificado = table.Column<decimal>(type: "decimal(28,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobacionSublimacion", x => x.id);
                    table.CheckConstraint("CK_Comprobacion_monto", "([montoVerificado]>=(0))");
                    table.ForeignKey(
                        name: "FK_ComprobacionSublimacion_Usuario_responsable",
                        column: x => x.responsable,
                        principalTable: "Usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ConstanciaEntrega",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pedido = table.Column<int>(type: "int", nullable: false),
                    decision = table.Column<bool>(type: "bit", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    responsable = table.Column<int>(type: "int", nullable: false),
                    referencia = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstanciaEntrega", x => x.id);
                    table.CheckConstraint("CK_ConstanciaEntrega_referencia", "(len(Trim([referencia]))>(0))");
                    table.ForeignKey(
                        name: "FK_ConstanciaEntrega_Usuario_responsable",
                        column: x => x.responsable,
                        principalTable: "Usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "DetalleCotizacion",
                columns: table => new
                {
                    versionCotizacion = table.Column<int>(type: "int", nullable: false),
                    detalle = table.Column<int>(type: "int", nullable: false),
                    precioUnitarioAcordado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    versionDiseno = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleCotizacion", x => new { x.versionCotizacion, x.detalle });
                    table.CheckConstraint("CK_DetalleCotizacion_importes", "([precioUnitarioAcordado]>(0))");
                });

            migrationBuilder.CreateTable(
                name: "DetallePedido",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pedido = table.Column<int>(type: "int", nullable: false),
                    producto = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    talla = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    tela = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    empaque = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ideaDiseno = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    archivoDisenoURL = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    precioBaseReferencia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    versionDisenoVigente = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallePedido", x => x.id);
                    table.CheckConstraint("CK_Detalle_cantidad", "([cantidad]>(0))");
                    table.CheckConstraint("CK_Detalle_diseno", "([archivoDisenoURL] IS NOT NULL AND len(Trim([archivoDisenoURL]))>(0) OR [ideaDiseno] IS NOT NULL AND len(Trim([ideaDiseno]))>(0))");
                    table.CheckConstraint("CK_Detalle_precioReferencia", "([precioBaseReferencia]>(0))");
                    table.CheckConstraint("CK_Detalle_textos", "(len(Trim([talla]))>(0) AND len(Trim([color]))>(0) AND len(Trim([tela]))>(0) AND len(Trim([empaque]))>(0))");
                    table.ForeignKey(
                        name: "FK_DetallePedido_Producto_producto",
                        column: x => x.producto,
                        principalTable: "Producto",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "VersionDiseno",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    numeroVersion = table.Column<int>(type: "int", nullable: false),
                    detalle = table.Column<int>(type: "int", nullable: false),
                    archivoURL = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    fechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    responsableRegistro = table.Column<int>(type: "int", nullable: false),
                    decision = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Pendiente")
                        .Annotation("Relational:DefaultConstraintName", "DF_VersionDiseno_decision"),
                    fechaDecision = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    responsableDecision = table.Column<int>(type: "int", nullable: true),
                    referenciaDecision = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionDiseno", x => x.id);
                    table.CheckConstraint("CK_VersionDiseno_contenido", "([archivoURL] IS NOT NULL AND len(Trim([archivoURL]))>(0) OR [descripcion] IS NOT NULL AND len(Trim([descripcion]))>(0))");
                    table.CheckConstraint("CK_VersionDiseno_decision", "([decision]=N'Pendiente' AND [fechaDecision] IS NULL AND [responsableDecision] IS NULL AND [referenciaDecision] IS NULL OR ([decision]=N'Rechazada' OR [decision]=N'Aprobada') AND [fechaDecision] IS NOT NULL AND [fechaDecision]>=[fechaRegistro] AND [responsableDecision] IS NOT NULL AND [referenciaDecision] IS NOT NULL AND len(Trim([referenciaDecision]))>(0))");
                    table.CheckConstraint("CK_VersionDiseno_numero", "([numeroVersion]>(0))");
                    table.ForeignKey(
                        name: "FK_VersionDiseno_DetallePedido_detalle",
                        column: x => x.detalle,
                        principalTable: "DetallePedido",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_VersionDiseno_Usuario_responsableDecision",
                        column: x => x.responsableDecision,
                        principalTable: "Usuario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_VersionDiseno_Usuario_responsableRegistro",
                        column: x => x.responsableRegistro,
                        principalTable: "Usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "DisenoComprobado",
                columns: table => new
                {
                    comprobacion = table.Column<int>(type: "int", nullable: false),
                    versionDiseno = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisenoComprobado", x => new { x.comprobacion, x.versionDiseno });
                    table.ForeignKey(
                        name: "FK_DisenoComprobado_ComprobacionSublimacion_comprobacion",
                        column: x => x.comprobacion,
                        principalTable: "ComprobacionSublimacion",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_DisenoComprobado_VersionDiseno_versionDiseno",
                        column: x => x.versionDiseno,
                        principalTable: "VersionDiseno",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pedido = table.Column<int>(type: "int", nullable: false),
                    monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    medio = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    estadoVerificacion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Pendiente")
                        .Annotation("Relational:DefaultConstraintName", "DF_Pago_estado"),
                    comprobanteURL = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    fechaVerificacion = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    responsableVerificacion = table.Column<int>(type: "int", nullable: true),
                    confirmacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    controlConcurrencia = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.id);
                    table.CheckConstraint("CK_Pago_medio", "([medio]=N'Tarjeta' OR [medio]=N'Depósito' OR [medio]=N'Transferencia' OR [medio]=N'Efectivo')");
                    table.CheckConstraint("CK_Pago_monto", "([monto]>(0))");
                    table.CheckConstraint("CK_Pago_referencia", "([estadoVerificacion]<>N'Verificado' OR [referencia] IS NOT NULL AND len(Trim([referencia]))>(0))");
                    table.CheckConstraint("CK_Pago_resolucion", "([estadoVerificacion]=N'Pendiente' AND [fechaVerificacion] IS NULL AND [responsableVerificacion] IS NULL AND [comprobanteURL] IS NOT NULL AND len(Trim([comprobanteURL]))>(0) OR ([estadoVerificacion]=N'Rechazado' OR [estadoVerificacion]=N'Verificado') AND [fechaVerificacion] IS NOT NULL AND [responsableVerificacion] IS NOT NULL)");
                    table.CheckConstraint("CK_Pago_respaldo", "([comprobanteURL] IS NOT NULL AND len(Trim([comprobanteURL]))>(0) OR [estadoVerificacion]=N'Verificado' AND [referencia] IS NOT NULL AND len(Trim([referencia]))>(0))");
                    table.ForeignKey(
                        name: "FK_Pago_Usuario_responsableVerificacion",
                        column: x => x.responsableVerificacion,
                        principalTable: "Usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Pedido",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    cliente = table.Column<int>(type: "int", nullable: false),
                    fechaOrden = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    fechaEntregaAcordada = table.Column<DateOnly>(type: "date", nullable: true),
                    fechaEntregaReal = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pendiente")
                        .Annotation("Relational:DefaultConstraintName", "DF_Pedido_estado"),
                    modalidadEntrega = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    direccionEntrega = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    versionCotizacionVigente = table.Column<int>(type: "int", nullable: true),
                    condicionAdministrativa = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Activa")
                        .Annotation("Relational:DefaultConstraintName", "DF_Pedido_condicion"),
                    motivoArchivo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    fechaArchivo = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    responsableArchivo = table.Column<int>(type: "int", nullable: true),
                    confirmacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    huellaSolicitud = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    controlConcurrencia = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedido", x => x.id);
                    table.CheckConstraint("CK_Pedido_archivo", "([condicionAdministrativa]=N'Activa' AND [motivoArchivo] IS NULL AND [fechaArchivo] IS NULL AND [responsableArchivo] IS NULL OR [condicionAdministrativa]=N'Archivada' AND [estado]=N'Pendiente' AND [versionCotizacionVigente] IS NULL AND [motivoArchivo] IS NOT NULL AND len(Trim([motivoArchivo]))>(0) AND [fechaArchivo] IS NOT NULL AND [responsableArchivo] IS NOT NULL)");
                    table.CheckConstraint("CK_Pedido_codigo", "(len(Trim([codigo]))>(0))");
                    table.CheckConstraint("CK_Pedido_direccion", "([modalidadEntrega]=N'Retiro' OR [direccionEntrega] IS NOT NULL AND len(Trim([direccionEntrega]))>(0))");
                    table.CheckConstraint("CK_Pedido_entrega", "([estado]=N'Entregado' AND [fechaEntregaReal] IS NOT NULL OR [estado]<>N'Entregado' AND [fechaEntregaReal] IS NULL)");
                    table.CheckConstraint("CK_Pedido_estado", "([estado]=N'Entregado' OR [estado]=N'Listo para entrega' OR [estado]=N'En producción' OR [estado]=N'Pendiente')");
                    table.CheckConstraint("CK_Pedido_modalidad", "([modalidadEntrega]=N'Domicilio' OR [modalidadEntrega]=N'Retiro')");
                    table.ForeignKey(
                        name: "FK_Pedido_Usuario_cliente",
                        column: x => x.cliente,
                        principalTable: "Usuario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Pedido_Usuario_responsableArchivo",
                        column: x => x.responsableArchivo,
                        principalTable: "Usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "VersionCotizacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    numeroVersion = table.Column<int>(type: "int", nullable: false),
                    pedido = table.Column<int>(type: "int", nullable: false),
                    fechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    responsableRegistro = table.Column<int>(type: "int", nullable: false),
                    decision = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Propuesta")
                        .Annotation("Relational:DefaultConstraintName", "DF_VersionCotizacion_decision"),
                    fechaDecision = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    responsableDecision = table.Column<int>(type: "int", nullable: true),
                    referenciaDecision = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionCotizacion", x => x.id);
                    table.CheckConstraint("CK_VersionCotizacion_decision", "([decision]=N'Propuesta' AND [fechaDecision] IS NULL AND [responsableDecision] IS NULL AND [referenciaDecision] IS NULL OR ([decision]=N'Rechazada' OR [decision]=N'Aceptada') AND [fechaDecision] IS NOT NULL AND [fechaDecision]>=[fechaRegistro] AND [responsableDecision] IS NOT NULL AND [referenciaDecision] IS NOT NULL AND len(Trim([referenciaDecision]))>(0))");
                    table.CheckConstraint("CK_VersionCotizacion_numero", "([numeroVersion]>(0))");
                    table.ForeignKey(
                        name: "FK_VersionCotizacion_Pedido_pedido",
                        column: x => x.pedido,
                        principalTable: "Pedido",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_VersionCotizacion_Usuario_responsableDecision",
                        column: x => x.responsableDecision,
                        principalTable: "Usuario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_VersionCotizacion_Usuario_responsableRegistro",
                        column: x => x.responsableRegistro,
                        principalTable: "Usuario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprobacionSublimacion_responsable",
                table: "ComprobacionSublimacion",
                column: "responsable");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobacionSublimacion_versionCotizacion",
                table: "ComprobacionSublimacion",
                column: "versionCotizacion");

            migrationBuilder.CreateIndex(
                name: "IX_ConstanciaEntrega_pedido",
                table: "ConstanciaEntrega",
                column: "pedido");

            migrationBuilder.CreateIndex(
                name: "IX_ConstanciaEntrega_responsable",
                table: "ConstanciaEntrega",
                column: "responsable");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCotizacion_detalle",
                table: "DetalleCotizacion",
                column: "detalle");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCotizacion_versionDiseno",
                table: "DetalleCotizacion",
                column: "versionDiseno");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePedido_pedido",
                table: "DetallePedido",
                column: "pedido");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePedido_producto",
                table: "DetallePedido",
                column: "producto");

            migrationBuilder.CreateIndex(
                name: "IX_DetallePedido_versionDisenoVigente",
                table: "DetallePedido",
                column: "versionDisenoVigente");

            migrationBuilder.CreateIndex(
                name: "IX_DisenoComprobado_versionDiseno",
                table: "DisenoComprobado",
                column: "versionDiseno");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_pedido_estado",
                table: "Pago",
                columns: new[] { "pedido", "estadoVerificacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Pago_pedido_estadoVerificacion",
                table: "Pago",
                columns: new[] { "pedido", "estadoVerificacion" })
                .Annotation("SqlServer:Include", new[] { "monto" });

            migrationBuilder.CreateIndex(
                name: "IX_Pago_responsableVerificacion",
                table: "Pago",
                column: "responsableVerificacion");

            migrationBuilder.CreateIndex(
                name: "UQ_Pago_confirmacion",
                table: "Pago",
                columns: new[] { "pedido", "confirmacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Pago_referenciaVerificada",
                table: "Pago",
                columns: new[] { "pedido", "medio", "referencia" },
                unique: true,
                filter: "([estadoVerificacion]=N'Verificado')");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_cliente_fecha",
                table: "Pedido",
                columns: new[] { "cliente", "fechaOrden" });

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_responsableArchivo",
                table: "Pedido",
                column: "responsableArchivo");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_versionCotizacionVigente",
                table: "Pedido",
                column: "versionCotizacionVigente");

            migrationBuilder.CreateIndex(
                name: "UQ_Pedido_codigo",
                table: "Pedido",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Pedido_confirmacion",
                table: "Pedido",
                columns: new[] { "cliente", "confirmacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Usuario_correo",
                table: "Usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionCotizacion_responsableDecision",
                table: "VersionCotizacion",
                column: "responsableDecision");

            migrationBuilder.CreateIndex(
                name: "IX_VersionCotizacion_responsableRegistro",
                table: "VersionCotizacion",
                column: "responsableRegistro");

            migrationBuilder.CreateIndex(
                name: "UQ_VersionCotizacion_numero",
                table: "VersionCotizacion",
                columns: new[] { "pedido", "numeroVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionDiseno_responsableDecision",
                table: "VersionDiseno",
                column: "responsableDecision");

            migrationBuilder.CreateIndex(
                name: "IX_VersionDiseno_responsableRegistro",
                table: "VersionDiseno",
                column: "responsableRegistro");

            migrationBuilder.CreateIndex(
                name: "UQ_VersionDiseno_numero",
                table: "VersionDiseno",
                columns: new[] { "detalle", "numeroVersion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobacionSublimacion_VersionCotizacion_versionCotizacion",
                table: "ComprobacionSublimacion",
                column: "versionCotizacion",
                principalTable: "VersionCotizacion",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConstanciaEntrega_Pedido_pedido",
                table: "ConstanciaEntrega",
                column: "pedido",
                principalTable: "Pedido",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleCotizacion_DetallePedido_detalle",
                table: "DetalleCotizacion",
                column: "detalle",
                principalTable: "DetallePedido",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleCotizacion_VersionCotizacion_versionCotizacion",
                table: "DetalleCotizacion",
                column: "versionCotizacion",
                principalTable: "VersionCotizacion",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleCotizacion_VersionDiseno_versionDiseno",
                table: "DetalleCotizacion",
                column: "versionDiseno",
                principalTable: "VersionDiseno",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallePedido_Pedido_pedido",
                table: "DetallePedido",
                column: "pedido",
                principalTable: "Pedido",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_versionVigente",
                table: "DetallePedido",
                column: "versionDisenoVigente",
                principalTable: "VersionDiseno",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pago_Pedido_pedido",
                table: "Pago",
                column: "pedido",
                principalTable: "Pedido",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedido_versionVigente",
                table: "Pedido",
                column: "versionCotizacionVigente",
                principalTable: "VersionCotizacion",
                principalColumn: "id");
            InicialComplementos.Crear(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            InicialComplementos.EliminarVistas(migrationBuilder);
            migrationBuilder.DropForeignKey(
                name: "FK_Pedido_Usuario_cliente",
                table: "Pedido");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedido_Usuario_responsableArchivo",
                table: "Pedido");

            migrationBuilder.DropForeignKey(
                name: "FK_VersionCotizacion_Usuario_responsableDecision",
                table: "VersionCotizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_VersionCotizacion_Usuario_responsableRegistro",
                table: "VersionCotizacion");

            migrationBuilder.DropForeignKey(
                name: "FK_VersionDiseno_Usuario_responsableDecision",
                table: "VersionDiseno");

            migrationBuilder.DropForeignKey(
                name: "FK_VersionDiseno_Usuario_responsableRegistro",
                table: "VersionDiseno");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedido_versionVigente",
                table: "Pedido");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallePedido_Pedido_pedido",
                table: "DetallePedido");

            migrationBuilder.DropForeignKey(
                name: "FK_VersionDiseno_DetallePedido_detalle",
                table: "VersionDiseno");

            migrationBuilder.DropTable(
                name: "ConstanciaEntrega");

            migrationBuilder.DropTable(
                name: "DetalleCotizacion");

            migrationBuilder.DropTable(
                name: "DisenoComprobado");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "ComprobacionSublimacion");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "VersionCotizacion");

            migrationBuilder.DropTable(
                name: "Pedido");

            migrationBuilder.DropTable(
                name: "DetallePedido");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "VersionDiseno");
        }
    }
}
