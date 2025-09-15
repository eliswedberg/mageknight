using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MageKnightOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddTileEdgeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EdgeData_EdgeSymbols",
                table: "BoardTiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "EdgeData_EdgeTerrains",
                table: "BoardTiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "CombatResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CombatId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefenderId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackerTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    DefenderTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Winner = table.Column<int>(type: "INTEGER", nullable: true),
                    DamageDealt = table.Column<int>(type: "INTEGER", nullable: false),
                    IsVictory = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatResults_Combats_CombatId",
                        column: x => x.CombatId,
                        principalTable: "Combats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CombatResults_CombatId",
                table: "CombatResults",
                column: "CombatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombatResults");

            migrationBuilder.DropColumn(
                name: "EdgeData_EdgeSymbols",
                table: "BoardTiles");

            migrationBuilder.DropColumn(
                name: "EdgeData_EdgeTerrains",
                table: "BoardTiles");
        }
    }
}
