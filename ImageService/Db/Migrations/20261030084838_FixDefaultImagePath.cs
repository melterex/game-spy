using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageService.Migrations
{
    public partial class FixDefaultImagePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE images
                SET "Path" = '/samples/default.png'
                WHERE "Id" = 1
                  AND "Hash" = '5405d77c51fb46a0cbf26cb96fe4da4d'
                  AND "Path" = '/Samples/default.png'
                  AND "Extension" = 'png'
                  AND "Source" = 1;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE images
                SET "Path" = '/Samples/default.png'
                WHERE "Id" = 1
                  AND "Hash" = '5405d77c51fb46a0cbf26cb96fe4da4d'
                  AND "Path" = '/samples/default.png'
                  AND "Extension" = 'png'
                  AND "Source" = 1;
                """);
        }
    }
}
