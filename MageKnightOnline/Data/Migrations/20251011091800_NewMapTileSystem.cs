using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MageKnightOnline.Migrations
{
    /// <inheritdoc />
    public partial class NewMapTileSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MapGraphs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Edges = table.Column<string>(type: "jsonb", nullable: false),
                    CoastlineMask = table.Column<string>(type: "jsonb", nullable: false),
                    ScenarioId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartingLayout = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                    CurrentPhase = table.Column<int>(type: "INTEGER", nullable: false),
                    CountrysideTilesRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    CoreNonCityTilesRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    CoreCityTilesRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentTurn = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentRound = table.Column<int>(type: "INTEGER", nullable: false),
                    IsInitialized = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfigurationData = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapGraphs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapGraphs_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapTileNews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TileId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TileType = table.Column<int>(type: "INTEGER", nullable: false),
                    CenterQ = table.Column<int>(type: "INTEGER", nullable: false),
                    CenterR = table.Column<int>(type: "INTEGER", nullable: false),
                    Orientation = table.Column<int>(type: "INTEGER", nullable: false),
                    AdjacentTileIds = table.Column<string>(type: "jsonb", nullable: false),
                    IsRevealed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPlaced = table.Column<bool>(type: "INTEGER", nullable: false),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BackColor = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCity = table.Column<bool>(type: "INTEGER", nullable: false),
                    CityLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    CityColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlacementOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    PlacementValidationData = table.Column<string>(type: "jsonb", nullable: false),
                    MapGraphId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapTileNews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapTileNews_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MapTileNews_MapGraphs_MapGraphId",
                        column: x => x.MapGraphId,
                        principalTable: "MapGraphs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MapTileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsConquered = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConqueredByPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ConqueredOnTurn = table.Column<int>(type: "INTEGER", nullable: true),
                    Defenders = table.Column<string>(type: "jsonb", nullable: false),
                    Rewards = table.Column<string>(type: "jsonb", nullable: false),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MapGraphId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_GamePlayers_ConqueredByPlayerId",
                        column: x => x.ConqueredByPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cities_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cities_MapGraphs_MapGraphId",
                        column: x => x.MapGraphId,
                        principalTable: "MapGraphs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cities_MapTileNews_MapTileId",
                        column: x => x.MapTileId,
                        principalTable: "MapTileNews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HexSpaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HexId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Q = table.Column<int>(type: "INTEGER", nullable: false),
                    R = table.Column<int>(type: "INTEGER", nullable: false),
                    TerrainType = table.Column<int>(type: "INTEGER", nullable: false),
                    SiteId = table.Column<int>(type: "INTEGER", nullable: true),
                    OccupantData = table.Column<string>(type: "jsonb", nullable: true),
                    MapTileId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionInTile = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementCost = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRevealed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExplored = table.Column<bool>(type: "INTEGER", nullable: false),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HexSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HexSpaces_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HexSpaces_MapTileNews_MapTileId",
                        column: x => x.MapTileId,
                        principalTable: "MapTileNews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HexSpaces_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_ConqueredByPlayerId",
                table: "Cities",
                column: "ConqueredByPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_GameSessionId",
                table: "Cities",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_MapGraphId",
                table: "Cities",
                column: "MapGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_MapTileId",
                table: "Cities",
                column: "MapTileId");

            migrationBuilder.CreateIndex(
                name: "IX_HexSpaces_GameSessionId_Q_R",
                table: "HexSpaces",
                columns: new[] { "GameSessionId", "Q", "R" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HexSpaces_MapTileId",
                table: "HexSpaces",
                column: "MapTileId");

            migrationBuilder.CreateIndex(
                name: "IX_HexSpaces_SiteId",
                table: "HexSpaces",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_MapGraphs_GameSessionId",
                table: "MapGraphs",
                column: "GameSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapTileNews_GameSessionId_TileId",
                table: "MapTileNews",
                columns: new[] { "GameSessionId", "TileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapTileNews_MapGraphId",
                table: "MapTileNews",
                column: "MapGraphId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "HexSpaces");

            migrationBuilder.DropTable(
                name: "MapTileNews");

            migrationBuilder.DropTable(
                name: "MapGraphs");
        }
    }
}
