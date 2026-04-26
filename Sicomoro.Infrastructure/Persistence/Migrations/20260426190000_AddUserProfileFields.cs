using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Sicomoro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SicomoroDbContext))]
    [Migration("20260426190000_AddUserProfileFields")]
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CiNit",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CiNit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Users");
        }
    }
}
