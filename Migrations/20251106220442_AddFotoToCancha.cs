using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistReservasDeportivas.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoToCancha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Foto",
                table: "Canchas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Foto",
                table: "Canchas");
        }
    }
}
