using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MageKnightOnline.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedTurnCardSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CombatActions_GamePlayers_PlayerId",
                table: "CombatActions");

            migrationBuilder.DropForeignKey(
                name: "FK_CombatActions_MageKnightCards_CardId",
                table: "CombatActions");

            migrationBuilder.DropForeignKey(
                name: "FK_CombatParticipants_SiteEnemies_EnemyId",
                table: "CombatParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Combats_GamePlayers_AttackingPlayerId",
                table: "Combats");

            migrationBuilder.DropForeignKey(
                name: "FK_Combats_GamePlayers_DefendingPlayerId",
                table: "Combats");

            migrationBuilder.DropForeignKey(
                name: "FK_Combats_Sites_DefendingSiteId",
                table: "Combats");

            migrationBuilder.DropForeignKey(
                name: "FK_MapTiles_TileDecks_TileDeckId1",
                table: "MapTiles");

            migrationBuilder.DropIndex(
                name: "IX_MapTiles_TileDeckId1",
                table: "MapTiles");

            migrationBuilder.DropIndex(
                name: "IX_Combats_AttackingPlayerId",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_Combats_DefendingPlayerId",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_Combats_DefendingSiteId",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_CombatActions_CardId",
                table: "CombatActions");

            migrationBuilder.DropIndex(
                name: "IX_CombatActions_PlayerId",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "TileDeckId1",
                table: "MapTiles");

            migrationBuilder.DropColumn(
                name: "DefendingPlayerId",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "ActionSequence",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "AttackValue",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "BlockValue",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "CombatActions");

            migrationBuilder.RenameColumn(
                name: "TurnNumber",
                table: "Combats",
                newName: "SiteId");

            migrationBuilder.RenameColumn(
                name: "DefendingSiteId",
                table: "Combats",
                newName: "CurrentParticipantId");

            migrationBuilder.RenameColumn(
                name: "AttackingPlayerId",
                table: "Combats",
                newName: "CurrentTurn");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "CombatActions",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "RangeValue",
                table: "CombatActions",
                newName: "ParticipantId");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "CombatActions",
                newName: "ActionType");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "CombatActions",
                newName: "TargetId");

            migrationBuilder.AddColumn<int>(
                name: "ActionPointsCost",
                table: "TurnActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttackPointsCost",
                table: "TurnActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlockPointsCost",
                table: "TurnActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CardId",
                table: "TurnActions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InfluencePointsCost",
                table: "TurnActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "TurnActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MovementPointsCost",
                table: "TurnActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "TurnActions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DifficultyLevel",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EnemyIds",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRepeatable",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LootTable",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredLevel",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SiteSubType",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpecialRequirements",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcquisitionMethod",
                table: "MageKnightCards",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttackPoints",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlockPoints",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CardSubType",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CrystalCost",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FameValue",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InfluencePoints",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPermanent",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlayable",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ManaCost",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MovePoints",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReputationValue",
                table: "MageKnightCards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpecialEffects",
                table: "MageKnightCards",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttackPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlockPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasPassed",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "InfluencePoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxActionPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttackPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxBlockPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxInfluencePoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxMovementPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MovementPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedActionPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedAttackPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedBlockPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedInfluencePoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedMovementPoints",
                table: "GameTurns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentCrystals",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentHealth",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentMana",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnemiesDefeated",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EquippedArtifacts",
                table: "GamePlayers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Experience",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceToNextLevel",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxCrystals",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxHealth",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxMana",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SitesConquered",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalFameEarned",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReputationEarned",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UnlockedAbilities",
                table: "GamePlayers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CombatParticipants",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CombatActions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "Enemies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackValue = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    Initiative = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialAbilities = table.Column<string>(type: "TEXT", nullable: true),
                    LootTable = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enemies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnhancedPlayerHands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxHandSize = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnhancedPlayerHands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnhancedPlayerHands_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnhancedPlayerHands_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCardAcquisitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CardId = table.Column<int>(type: "INTEGER", nullable: false),
                    GameSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AcquisitionMethod = table.Column<string>(type: "TEXT", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsInHand = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsInDeck = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsInDiscard = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPermanent = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnhancedPlayerHandId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCardAcquisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerCardAcquisitions_EnhancedPlayerHands_EnhancedPlayerHandId",
                        column: x => x.EnhancedPlayerHandId,
                        principalTable: "EnhancedPlayerHands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerCardAcquisitions_GamePlayers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerCardAcquisitions_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerCardAcquisitions_MageKnightCards_CardId",
                        column: x => x.CardId,
                        principalTable: "MageKnightCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TurnActions_CardId",
                table: "TurnActions",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_Combats_SiteId",
                table: "Combats",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActions_ParticipantId",
                table: "CombatActions",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancedPlayerHands_GameSessionId",
                table: "EnhancedPlayerHands",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancedPlayerHands_PlayerId",
                table: "EnhancedPlayerHands",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCardAcquisitions_CardId",
                table: "PlayerCardAcquisitions",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCardAcquisitions_EnhancedPlayerHandId",
                table: "PlayerCardAcquisitions",
                column: "EnhancedPlayerHandId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCardAcquisitions_GameSessionId",
                table: "PlayerCardAcquisitions",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCardAcquisitions_PlayerId",
                table: "PlayerCardAcquisitions",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CombatActions_CombatParticipants_ParticipantId",
                table: "CombatActions",
                column: "ParticipantId",
                principalTable: "CombatParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CombatParticipants_Enemies_EnemyId",
                table: "CombatParticipants",
                column: "EnemyId",
                principalTable: "Enemies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Combats_Sites_SiteId",
                table: "Combats",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TurnActions_MageKnightCards_CardId",
                table: "TurnActions",
                column: "CardId",
                principalTable: "MageKnightCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CombatActions_CombatParticipants_ParticipantId",
                table: "CombatActions");

            migrationBuilder.DropForeignKey(
                name: "FK_CombatParticipants_Enemies_EnemyId",
                table: "CombatParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Combats_Sites_SiteId",
                table: "Combats");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnActions_MageKnightCards_CardId",
                table: "TurnActions");

            migrationBuilder.DropTable(
                name: "Enemies");

            migrationBuilder.DropTable(
                name: "PlayerCardAcquisitions");

            migrationBuilder.DropTable(
                name: "EnhancedPlayerHands");

            migrationBuilder.DropIndex(
                name: "IX_TurnActions_CardId",
                table: "TurnActions");

            migrationBuilder.DropIndex(
                name: "IX_Combats_SiteId",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_CombatActions_ParticipantId",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "ActionPointsCost",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "AttackPointsCost",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "BlockPointsCost",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "InfluencePointsCost",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "MovementPointsCost",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "TurnActions");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "EnemyIds",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "IsRepeatable",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "LootTable",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "RequiredLevel",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SiteSubType",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SpecialRequirements",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "AcquisitionMethod",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "AttackPoints",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "BlockPoints",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "CardSubType",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "CrystalCost",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "FameValue",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "InfluencePoints",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "IsPermanent",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "IsPlayable",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "ManaCost",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "MovePoints",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "ReputationValue",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "SpecialEffects",
                table: "MageKnightCards");

            migrationBuilder.DropColumn(
                name: "ActionPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "AttackPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "BlockPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "HasPassed",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "InfluencePoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "MaxActionPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "MaxAttackPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "MaxBlockPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "MaxInfluencePoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "MaxMovementPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "MovementPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "UsedActionPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "UsedAttackPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "UsedBlockPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "UsedInfluencePoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "UsedMovementPoints",
                table: "GameTurns");

            migrationBuilder.DropColumn(
                name: "CurrentCrystals",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "CurrentHealth",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "CurrentMana",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "EnemiesDefeated",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "EquippedArtifacts",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "ExperienceToNextLevel",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "MaxCrystals",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "MaxHealth",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "MaxMana",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "SitesConquered",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "TotalFameEarned",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "TotalReputationEarned",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "UnlockedAbilities",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CombatParticipants");

            migrationBuilder.RenameColumn(
                name: "SiteId",
                table: "Combats",
                newName: "TurnNumber");

            migrationBuilder.RenameColumn(
                name: "CurrentTurn",
                table: "Combats",
                newName: "AttackingPlayerId");

            migrationBuilder.RenameColumn(
                name: "CurrentParticipantId",
                table: "Combats",
                newName: "DefendingSiteId");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "CombatActions",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "CombatActions",
                newName: "CardId");

            migrationBuilder.RenameColumn(
                name: "ParticipantId",
                table: "CombatActions",
                newName: "RangeValue");

            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "CombatActions",
                newName: "PlayerId");

            migrationBuilder.AddColumn<int>(
                name: "TileDeckId1",
                table: "MapTiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefendingPlayerId",
                table: "Combats",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CombatActions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionSequence",
                table: "CombatActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttackValue",
                table: "CombatActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlockValue",
                table: "CombatActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "CombatActions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapTiles_TileDeckId1",
                table: "MapTiles",
                column: "TileDeckId1");

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
                name: "IX_CombatActions_CardId",
                table: "CombatActions",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActions_PlayerId",
                table: "CombatActions",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CombatActions_GamePlayers_PlayerId",
                table: "CombatActions",
                column: "PlayerId",
                principalTable: "GamePlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CombatActions_MageKnightCards_CardId",
                table: "CombatActions",
                column: "CardId",
                principalTable: "MageKnightCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CombatParticipants_SiteEnemies_EnemyId",
                table: "CombatParticipants",
                column: "EnemyId",
                principalTable: "SiteEnemies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Combats_GamePlayers_AttackingPlayerId",
                table: "Combats",
                column: "AttackingPlayerId",
                principalTable: "GamePlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Combats_GamePlayers_DefendingPlayerId",
                table: "Combats",
                column: "DefendingPlayerId",
                principalTable: "GamePlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Combats_Sites_DefendingSiteId",
                table: "Combats",
                column: "DefendingSiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MapTiles_TileDecks_TileDeckId1",
                table: "MapTiles",
                column: "TileDeckId1",
                principalTable: "TileDecks",
                principalColumn: "Id");
        }
    }
}
