using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UserManagement.Db.Migrations
{
    public partial class InitialUserManagementPostgreSql : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    provider = table.Column<string>(type: "text", nullable: true),
                    provider_key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_identity", "(provider IS NULL AND provider_key IS NULL AND password_hash IS NOT NULL) OR (provider IS NOT NULL AND length(btrim(provider)) > 0 AND provider_key IS NOT NULL AND length(btrim(provider_key)) > 0)");
                    table.CheckConstraint("ck_users_username", "length(btrim(username)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "game_results",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    game_id = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    is_winner = table.Column<bool>(type: "boolean", nullable: false),
                    game_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_results", x => x.id);
                    table.CheckConstraint("ck_game_results_game_id", "length(btrim(game_id)) > 0");
                    table.CheckConstraint("ck_game_results_role", "role IN ('Spy', 'Civilian')");
                    table.ForeignKey(
                        name: "FK_game_results_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_results_game_id",
                table: "game_results",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "ux_game_results_user_game",
                table: "game_results",
                columns: new[] { "user_id", "game_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_provider",
                table: "users",
                columns: new[] { "provider", "provider_key" },
                unique: true,
                filter: "provider IS NOT NULL AND provider_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_results");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
