using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistReservasDeportivas.Migrations
{
    /// <inheritdoc />
    public partial class AddEstadoYMetodoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Importe",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AddColumn<string>(
                name: "EstadoPago",
                table: "Pagos",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Pagos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoPago",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pagos");

            migrationBuilder.AlterColumn<decimal>(
                name: "Importe",
                table: "Pagos",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
