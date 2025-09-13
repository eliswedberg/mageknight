using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MageKnightOnline.Migrations
{
    /// <inheritdoc />
    public partial class CreateGameDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artifacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Block = table.Column<int>(type: "INTEGER", nullable: false),
                    Move = table.Column<int>(type: "INTEGER", nullable: false),
                    Influence = table.Column<int>(type: "INTEGER", nullable: false),
                    Fame = table.Column<int>(type: "INTEGER", nullable: false),
                    Reputation = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPermanent = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsConsumable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Uses = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialEffect = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Set = table.Column<string>(type: "TEXT", nullable: false),
                    Rarity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HostUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_AspNetUsers_HostUserId",
                        column: x => x.HostUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MageKnightCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Block = table.Column<int>(type: "INTEGER", nullable: false),
                    Move = table.Column<int>(type: "INTEGER", nullable: false),
                    Influence = table.Column<int>(type: "INTEGER", nullable: false),
                    Fame = table.Column<int>(type: "INTEGER", nullable: false),
                    Reputation = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSpell = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsArtifact = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnit = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Set = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MageKnightCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Spells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ManaCost = table.Column<int>(type: "INTEGER", nullable: false),
                    CrystalCost = table.Column<int>(type: "INTEGER", nullable: false),
                    Range = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Block = table.Column<int>(type: "INTEGER", nullable: false),
                    Move = table.Column<int>(type: "INTEGER", nullable: false),
                    Influence = table.Column<int>(type: "INTEGER", nullable: false),
                    Fame = table.Column<int>(type: "INTEGER", nullable: false),
                    Reputation = table.Column<int>(type: "INTEGER", nullable: false),
                    IsInstant = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPersistent = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpecialEffect = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Set = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spells", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Block = table.Column<int>(type: "INTEGER", nullable: false),
                    Move = table.Column<int>(type: "INTEGER", nullable: false),
                    Influence = table.Column<int>(type: "INTEGER", nullable: false),
                    Fame = table.Column<int>(type: "INTEGER", nullable: false),
                    Reputation = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    IsElite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMercenary = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMage = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpecialAbilities = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Set = table.Column<string>(type: "TEXT", nullable: false),
                    Rarity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameBoards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    MapType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameBoards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameBoards_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GamePlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PlayerNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    KnightName = table.Column<string>(type: "TEXT", nullable: true),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Fame = table.Column<int>(type: "INTEGER", nullable: false),
                    Reputation = table.Column<int>(type: "INTEGER", nullable: false),
                    Crystals = table.Column<int>(type: "INTEGER", nullable: false),
                    Mana = table.Column<int>(type: "INTEGER", nullable: false),
                    HandSize = table.Column<int>(type: "INTEGER", nullable: false),
                    DeckSize = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscardSize = table.Column<int>(type: "INTEGER", nullable: false),
                    Wounds = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCurrentPlayer = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPassed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamePlayers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GamePlayers_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TileDecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TileDecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TileDecks_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TurnNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionSequence = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameActions_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameActions_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TurnNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentPhase = table.Column<int>(type: "INTEGER", nullable: false),
                    GamePhase = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsNightPhase = table.Column<bool>(type: "INTEGER", nullable: false),
                    NightRounds = table.Column<int>(type: "INTEGER", nullable: false),
                    DayRounds = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentWeather = table.Column<string>(type: "TEXT", nullable: true),
                    GlobalFame = table.Column<int>(type: "INTEGER", nullable: false),
                    GlobalReputation = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameStates_GamePlayers_CurrentPlayerId",
                        column: x => x.CurrentPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameStates_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameTurns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TurnNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActionsRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    ManaAvailable = table.Column<int>(type: "INTEGER", nullable: false),
                    CrystalsAvailable = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTurns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameTurns_GamePlayers_CurrentPlayerId",
                        column: x => x.CurrentPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameTurns_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerArtifacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtifactId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEquipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsesRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerArtifacts_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerArtifacts_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CardId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeckPosition = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDrawn = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerDecks_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerDecks_MageKnightCards_CardId",
                        column: x => x.CardId,
                        principalTable: "MageKnightCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDiscards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CardId = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscardedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DiscardPosition = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDiscards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerDiscards_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerDiscards_MageKnightCards_CardId",
                        column: x => x.CardId,
                        principalTable: "MageKnightCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerHands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CardId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPlayed = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddedToHand = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HandPosition = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerHands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerHands_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerHands_MageKnightCards_CardId",
                        column: x => x.CardId,
                        principalTable: "MageKnightCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameBoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    MovedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MovementPointsUsed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerPositions_GameBoards_GameBoardId",
                        column: x => x.GameBoardId,
                        principalTable: "GameBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerPositions_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSpells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SpellId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLearned = table.Column<bool>(type: "INTEGER", nullable: false),
                    LearnedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsesRemaining = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSpells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSpells_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSpells_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRecruited = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecruitedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeployed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PositionX = table.Column<int>(type: "INTEGER", nullable: true),
                    PositionY = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentHealth = table.Column<int>(type: "INTEGER", nullable: false),
                    IsWounded = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDefeated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerUnits_GamePlayers_GamePlayerId",
                        column: x => x.GamePlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerUnits_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameBoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsExplored = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsConquered = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConqueredByPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ConqueredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AttackCost = table.Column<int>(type: "INTEGER", nullable: false),
                    FameReward = table.Column<int>(type: "INTEGER", nullable: false),
                    ReputationReward = table.Column<int>(type: "INTEGER", nullable: false),
                    CrystalsReward = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtifactReward = table.Column<string>(type: "TEXT", nullable: true),
                    SpellReward = table.Column<string>(type: "TEXT", nullable: true),
                    UnitReward = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sites_GameBoards_GameBoardId",
                        column: x => x.GameBoardId,
                        principalTable: "GameBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sites_GamePlayers_ConqueredByPlayerId",
                        column: x => x.ConqueredByPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MapTiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TileDeckId = table.Column<int>(type: "INTEGER", nullable: true),
                    TileNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCity = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImageName = table.Column<string>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlacedAtX = table.Column<int>(type: "INTEGER", nullable: true),
                    PlacedAtY = table.Column<int>(type: "INTEGER", nullable: true),
                    Rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    TileDeckId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapTiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapTiles_TileDecks_TileDeckId",
                        column: x => x.TileDeckId,
                        principalTable: "TileDecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MapTiles_TileDecks_TileDeckId1",
                        column: x => x.TileDeckId1,
                        principalTable: "TileDecks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GameEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameStateId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RoundNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TurnNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameEvents_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameEvents_GameStates_GameStateId",
                        column: x => x.GameStateId,
                        principalTable: "GameStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TurnActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameTurnId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActionSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUndoable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurnActions_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TurnActions_GameTurns_GameTurnId",
                        column: x => x.GameTurnId,
                        principalTable: "GameTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BoardTiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameBoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsExplored = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRevealed = table.Column<bool>(type: "INTEGER", nullable: false),
                    SiteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Terrain = table.Column<string>(type: "TEXT", nullable: true),
                    MovementCost = table.Column<int>(type: "INTEGER", nullable: false),
                    TileImageName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardTiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardTiles_GameBoards_GameBoardId",
                        column: x => x.GameBoardId,
                        principalTable: "GameBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardTiles_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Combats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackingPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefendingSiteId = table.Column<int>(type: "INTEGER", nullable: true),
                    DefendingPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TurnNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Combats_GamePlayers_AttackingPlayerId",
                        column: x => x.AttackingPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Combats_GamePlayers_DefendingPlayerId",
                        column: x => x.DefendingPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Combats_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Combats_Sites_DefendingSiteId",
                        column: x => x.DefendingSiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SiteEnemies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Block = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentHealth = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefeated = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpecialAbilities = table.Column<string>(type: "TEXT", nullable: true),
                    FameReward = table.Column<int>(type: "INTEGER", nullable: false),
                    ReputationReward = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteEnemies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteEnemies_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TileSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MapTileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Section = table.Column<int>(type: "INTEGER", nullable: false),
                    SiteType = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TileSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TileSites_MapTiles_MapTileId",
                        column: x => x.MapTileId,
                        principalTable: "MapTiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TileTerrainSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MapTileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Section = table.Column<int>(type: "INTEGER", nullable: false),
                    TerrainType = table.Column<string>(type: "TEXT", nullable: false),
                    MovementCost = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TileTerrainSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TileTerrainSections_MapTiles_MapTileId",
                        column: x => x.MapTileId,
                        principalTable: "MapTiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CombatActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CombatId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActionSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    CardId = table.Column<int>(type: "INTEGER", nullable: true),
                    AttackValue = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockValue = table.Column<int>(type: "INTEGER", nullable: false),
                    RangeValue = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatActions_Combats_CombatId",
                        column: x => x.CombatId,
                        principalTable: "Combats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombatActions_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombatActions_MageKnightCards_CardId",
                        column: x => x.CardId,
                        principalTable: "MageKnightCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CombatParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CombatId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    EnemyId = table.Column<int>(type: "INTEGER", nullable: true),
                    AttackValue = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentHealth = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefeated = table.Column<bool>(type: "INTEGER", nullable: false),
                    Initiative = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatParticipants_Combats_CombatId",
                        column: x => x.CombatId,
                        principalTable: "Combats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombatParticipants_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CombatParticipants_SiteEnemies_EnemyId",
                        column: x => x.EnemyId,
                        principalTable: "SiteEnemies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardTiles_GameBoardId",
                table: "BoardTiles",
                column: "GameBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardTiles_SiteId",
                table: "BoardTiles",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActions_CardId",
                table: "CombatActions",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActions_CombatId",
                table: "CombatActions",
                column: "CombatId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActions_PlayerId",
                table: "CombatActions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_CombatId",
                table: "CombatParticipants",
                column: "CombatId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_EnemyId",
                table: "CombatParticipants",
                column: "EnemyId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_PlayerId",
                table: "CombatParticipants",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Combats_AttackingPlayerId",
                table: "Combats",
                column: "AttackingPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Combats_DefendingPlayerId",
                table: "Combats",
                column: "DefendingPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Combats_DefendingSiteId",
                table: "Combats",
                column: "DefendingSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Combats_GameSessionId",
                table: "Combats",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameActions_GameSessionId",
                table: "GameActions",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameActions_PlayerId",
                table: "GameActions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameBoards_GameSessionId",
                table: "GameBoards",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_GameStateId",
                table: "GameEvents",
                column: "GameStateId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEvents_PlayerId",
                table: "GameEvents",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GameSessionId_PlayerNumber",
                table: "GamePlayers",
                columns: new[] { "GameSessionId", "PlayerNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GameSessionId_UserId",
                table: "GamePlayers",
                columns: new[] { "GameSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_UserId",
                table: "GamePlayers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_HostUserId",
                table: "GameSessions",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameStates_CurrentPlayerId",
                table: "GameStates",
                column: "CurrentPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameStates_GameSessionId",
                table: "GameStates",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameTurns_CurrentPlayerId",
                table: "GameTurns",
                column: "CurrentPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameTurns_GameSessionId",
                table: "GameTurns",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MapTiles_TileDeckId",
                table: "MapTiles",
                column: "TileDeckId");

            migrationBuilder.CreateIndex(
                name: "IX_MapTiles_TileDeckId1",
                table: "MapTiles",
                column: "TileDeckId1");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerArtifacts_ArtifactId",
                table: "PlayerArtifacts",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerArtifacts_GamePlayerId",
                table: "PlayerArtifacts",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDecks_CardId",
                table: "PlayerDecks",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDecks_GamePlayerId",
                table: "PlayerDecks",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDiscards_CardId",
                table: "PlayerDiscards",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDiscards_GamePlayerId",
                table: "PlayerDiscards",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHands_CardId",
                table: "PlayerHands",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHands_GamePlayerId",
                table: "PlayerHands",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPositions_GameBoardId",
                table: "PlayerPositions",
                column: "GameBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPositions_PlayerId",
                table: "PlayerPositions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSpells_GamePlayerId",
                table: "PlayerSpells",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSpells_SpellId",
                table: "PlayerSpells",
                column: "SpellId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUnits_GamePlayerId",
                table: "PlayerUnits",
                column: "GamePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUnits_UnitId",
                table: "PlayerUnits",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteEnemies_SiteId",
                table: "SiteEnemies",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_ConqueredByPlayerId",
                table: "Sites",
                column: "ConqueredByPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_GameBoardId",
                table: "Sites",
                column: "GameBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_TileDecks_GameSessionId",
                table: "TileDecks",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TileSites_MapTileId",
                table: "TileSites",
                column: "MapTileId");

            migrationBuilder.CreateIndex(
                name: "IX_TileTerrainSections_MapTileId",
                table: "TileTerrainSections",
                column: "MapTileId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnActions_GameTurnId",
                table: "TurnActions",
                column: "GameTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnActions_PlayerId",
                table: "TurnActions",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardTiles");

            migrationBuilder.DropTable(
                name: "CombatActions");

            migrationBuilder.DropTable(
                name: "CombatParticipants");

            migrationBuilder.DropTable(
                name: "GameActions");

            migrationBuilder.DropTable(
                name: "GameEvents");

            migrationBuilder.DropTable(
                name: "PlayerArtifacts");

            migrationBuilder.DropTable(
                name: "PlayerDecks");

            migrationBuilder.DropTable(
                name: "PlayerDiscards");

            migrationBuilder.DropTable(
                name: "PlayerHands");

            migrationBuilder.DropTable(
                name: "PlayerPositions");

            migrationBuilder.DropTable(
                name: "PlayerSpells");

            migrationBuilder.DropTable(
                name: "PlayerUnits");

            migrationBuilder.DropTable(
                name: "TileSites");

            migrationBuilder.DropTable(
                name: "TileTerrainSections");

            migrationBuilder.DropTable(
                name: "TurnActions");

            migrationBuilder.DropTable(
                name: "Combats");

            migrationBuilder.DropTable(
                name: "SiteEnemies");

            migrationBuilder.DropTable(
                name: "GameStates");

            migrationBuilder.DropTable(
                name: "Artifacts");

            migrationBuilder.DropTable(
                name: "MageKnightCards");

            migrationBuilder.DropTable(
                name: "Spells");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "MapTiles");

            migrationBuilder.DropTable(
                name: "GameTurns");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "TileDecks");

            migrationBuilder.DropTable(
                name: "GameBoards");

            migrationBuilder.DropTable(
                name: "GamePlayers");

            migrationBuilder.DropTable(
                name: "GameSessions");
        }
    }
}
