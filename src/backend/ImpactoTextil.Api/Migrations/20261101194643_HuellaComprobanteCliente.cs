using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactoTextil.Api.Migrations
{
    /// <inheritdoc />
    public partial class HuellaComprobanteCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "huellaComprobante",
                table: "Pago",
                type: "varbinary(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pago_huellaComprobante",
                table: "Pago",
                sql: "([huellaComprobante] IS NULL OR DATALENGTH([huellaComprobante]) = 32)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Pago_huellaComprobante",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "huellaComprobante",
                table: "Pago");
        }
    }
}
