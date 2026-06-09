using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicomoro.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(SicomoroDbContext))]
    [Migration("20260609193000_HardenSalesAccounting")]
    public partial class HardenSalesAccounting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Ventas" v
                SET "Estado" = 5, "ActualizadoEn" = NOW()
                WHERE v."Estado" = 1
                  AND EXISTS (
                      SELECT 1
                      FROM "Cobros" c
                      WHERE c."VentaId" = v."Id"
                        AND c."Estado" <> 5
                  );
                """);

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Estado" = 2, "ActualizadoEn" = NOW()
                WHERE "Email" = 'admin@sicomoro.local'
                  AND EXISTS (
                      SELECT 1
                      FROM "Users"
                      WHERE "Email" <> 'admin@sicomoro.local'
                        AND "Rol" = 1
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Ventas" v
                SET "Estado" = 1, "ActualizadoEn" = NOW()
                WHERE v."Estado" = 5
                  AND EXISTS (
                      SELECT 1
                      FROM "Cobros" c
                      WHERE c."VentaId" = v."Id"
                  );
                """);
        }
    }
}
