using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardsService.Migrations
{
    /// <inheritdoc />
    public partial class SyncMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Words",
                value: new List<string> { "Gymnasium", "Student", "Teacher", "Notebook", "Diploma", "Credit", "Lecture", "Journal", "Marker", "Briefcase", "Discipline", "University", "Exam" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Words",
                value: new List<string> { "Whistle", "Medal", "Timeout", "Fan", "Season Ticket", "Equipment", "Disqualification", "Overtime", "Grandstand", "Mascot", "Standard", "Rank", "Championship" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Words",
                value: new List<string> { "Ticket", "Suitcase", "Passport", "Layover", "Engine", "Route", "Flight attendant", "Fellow traveler", "Security check", "Hitchhiking", "Customs" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Words",
                value: new List<string> { "Rose", "Tulip", "Cactus", "Sunflower", "Daisy", "Birch", "Oak", "Palm Tree", "Fir Tree", "Bamboo", "Nettle", "Dandelion", "Lotus", "Fern", "Maple", "Lily", "Reed", "Aloe", "Vine", "Sequoia", "Moss", "Lilac", "Jasmine", "Lavender", "Orchid" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Words",
                value: new List<string> { "Lion", "Tiger", "Elephant", "Giraffe", "Zebra", "Panda", "Kangaroo", "Penguin", "Shark", "Dolphin", "Crocodile", "Turtle", "Snake", "Eagle", "Parrot", "Owl", "Bear", "Wolf", "Fox", "Squirrel", "Monkey", "Hippopotamus", "Rhinoceros", "Camel" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Words",
                value: new List<string> { "Gymnasium", "Student", "Teacher", "Notebook", "Diploma", "Credit", "Lecture", "Journal", "Marker", "Briefcase", "Discipline", "University", "Exam" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Words",
                value: new List<string> { "Whistle", "Medal", "Timeout", "Fan", "Season Ticket", "Equipment", "Disqualification", "Overtime", "Grandstand", "Mascot", "Standard", "Rank", "Championship" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Words",
                value: new List<string> { "Ticket", "Suitcase", "Passport", "Layover", "Engine", "Route", "Flight attendant", "Fellow traveler", "Security check", "Hitchhiking", "Customs" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Words",
                value: new List<string> { "Rose", "Tulip", "Cactus", "Sunflower", "Daisy", "Birch", "Oak", "Palm Tree", "Fir Tree", "Bamboo", "Nettle", "Dandelion", "Lotus", "Fern", "Maple", "Lily", "Reed", "Aloe", "Vine", "Sequoia", "Moss", "Lilac", "Jasmine", "Lavender", "Orchid" });

            migrationBuilder.UpdateData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Words",
                value: new List<string> { "Lion", "Tiger", "Elephant", "Giraffe", "Zebra", "Panda", "Kangaroo", "Penguin", "Shark", "Dolphin", "Crocodile", "Turtle", "Snake", "Eagle", "Parrot", "Owl", "Bear", "Wolf", "Fox", "Squirrel", "Monkey", "Hippopotamus", "Rhinoceros", "Camel" });
        }
    }
}
