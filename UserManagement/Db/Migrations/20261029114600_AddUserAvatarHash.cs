using authorization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagement.Db.Migrations;

[DbContext(typeof(UserDbContext))]
[Migration("20261029114600_AddUserAvatarHash")]
public class AddUserAvatarHash : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "avatar_hash",
            table: "users",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "avatar_hash",
            table: "users");
    }
}
