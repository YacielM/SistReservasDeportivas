using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistReservasDeportivas.Migrations
{
    /// <inheritdoc />
    public partial class AddCanceladaToReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Cancelada",
                table: "Reservas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cancelada",
                table: "Reservas");
        }
    }
}
