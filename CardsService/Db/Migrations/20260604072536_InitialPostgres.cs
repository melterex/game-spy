using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CardsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Theme = table.Column<string>(type: "text", nullable: false),
                    Words = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Themes",
                columns: new[] { "Id", "Theme", "Words" },
                values: new object[,]
                {
                    { 1, "Education", new List<string> { "Gymnasium", "Student", "Teacher", "Notebook", "Diploma", "Credit", "Lecture", "Journal", "Marker", "Briefcase", "Discipline", "University", "Exam" } },
                    { 2, "Sport", new List<string> { "Whistle", "Medal", "Timeout", "Fan", "Season Ticket", "Equipment", "Disqualification", "Overtime", "Grandstand", "Mascot", "Standard", "Rank", "Championship" } },
                    { 3, "Travelling", new List<string> { "Ticket", "Suitcase", "Passport", "Layover", "Engine", "Route", "Flight attendant", "Fellow traveler", "Security check", "Hitchhiking", "Customs" } },
                    { 4, "Plants", new List<string> { "Rose", "Tulip", "Cactus", "Sunflower", "Daisy", "Birch", "Oak", "Palm Tree", "Fir Tree", "Bamboo", "Nettle", "Dandelion", "Lotus", "Fern", "Maple", "Lily", "Reed", "Aloe", "Vine", "Sequoia", "Moss", "Lilac", "Jasmine", "Lavender", "Orchid" } },
                    { 5, "Animals", new List<string> { "Lion", "Tiger", "Elephant", "Giraffe", "Zebra", "Panda", "Kangaroo", "Penguin", "Shark", "Dolphin", "Crocodile", "Turtle", "Snake", "Eagle", "Parrot", "Owl", "Bear", "Wolf", "Fox", "Squirrel", "Monkey", "Hippopotamus", "Rhinoceros", "Camel" } }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Themes");
        }
    }
}
