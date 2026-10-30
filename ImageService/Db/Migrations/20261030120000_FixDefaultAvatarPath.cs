using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageService.Migrations;

public partial class FixDefaultAvatarPath : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "images",
            keyColumn: "Id",
            keyValue: 1,
            column: "Path",
            value: "/samples/default.png");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "images",
            keyColumn: "Id",
            keyValue: 1,
            column: "Path",
            value: "/Samples/default.png");
    }
}
