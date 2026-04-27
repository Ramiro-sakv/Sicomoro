using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicomoro.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(SicomoroDbContext))]
    [Migration("20260427124000_AddCatalogoAnuncios")]
    public partial class AddCatalogoAnuncios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnunciosCatalogo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductoMaderaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Subtitulo = table.Column<string>(type: "text", nullable: true),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    ImagenUrl = table.Column<string>(type: "text", nullable: true),
                    PrecioTexto = table.Column<string>(type: "text", nullable: true),
                    Etiqueta = table.Column<string>(type: "text", nullable: true),
                    CtaTexto = table.Column<string>(type: "text", nullable: true),
                    CtaUrl = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Publicado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnunciosCatalogo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnunciosCatalogo_ProductosMadera_ProductoMaderaId",
                        column: x => x.ProductoMaderaId,
                        principalTable: "ProductosMadera",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnunciosCatalogo_ProductoMaderaId",
                table: "AnunciosCatalogo",
                column: "ProductoMaderaId");

            migrationBuilder.CreateIndex(
                name: "IX_AnunciosCatalogo_Publicado_Orden",
                table: "AnunciosCatalogo",
                columns: new[] { "Publicado", "Orden" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AnunciosCatalogo");
        }
    }
}
