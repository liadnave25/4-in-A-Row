using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerApp.Migrations
{
    public partial class RebuildSchema_NoStringPlayerId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // מחיקה בטוחה אם הטבלאות קיימות (לפי סדר תלות)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.Moves', N'U') IS NOT NULL DROP TABLE dbo.Moves;
IF OBJECT_ID(N'dbo.Games', N'U') IS NOT NULL DROP TABLE dbo.Games;
IF OBJECT_ID(N'dbo.Players', N'U') IS NOT NULL DROP TABLE dbo.Players;
");

            // Players (כמו ב-InitialCreate: ללא Identity)
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            // Games (כמו ב-InitialCreate)
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    Moves = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Winner = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_PlayerId",
                table: "Games",
                column: "PlayerId");

            // Moves — כאן התיקון: PlayerId מסוג INT (לא NVARCHAR)
            migrationBuilder.CreateTable(
                name: "Moves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Column = table.Column<int>(type: "int", nullable: false),
                    MoveTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moves", x => x.Id);
                    // אם תרצה FK ל-Games/Players, אפשר להוסיף כאן.
                    // השארתי בלי, כדי לשמור התאמה ל-InitialCreate ולמנוע הפתעות אם המודל לא מגדיר ניווט.
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Moves");
            migrationBuilder.DropTable(name: "Games");
            migrationBuilder.DropTable(name: "Players");
        }
    }
}
