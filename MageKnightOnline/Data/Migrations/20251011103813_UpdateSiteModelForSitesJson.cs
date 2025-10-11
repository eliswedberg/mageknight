using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MageKnightOnline.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSiteModelForSitesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_GameBoards_GameBoardId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ArtifactReward",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "AttackCost",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "CrystalsReward",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "EnemyIds",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "FameReward",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "IsExplored",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "IsRepeatable",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "LootTable",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SpecialRequirements",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SpellReward",
                table: "Sites");

            migrationBuilder.RenameColumn(
                name: "Y",
                table: "Sites",
                newName: "IsRevealed");

            migrationBuilder.RenameColumn(
                name: "X",
                table: "Sites",
                newName: "IsFortified");

            migrationBuilder.RenameColumn(
                name: "UnitReward",
                table: "Sites",
                newName: "BurnedAt");

            migrationBuilder.RenameColumn(
                name: "SiteSubType",
                table: "Sites",
                newName: "IsBurned");

            migrationBuilder.RenameColumn(
                name: "RequiredLevel",
                table: "Sites",
                newName: "GameSessionId");

            migrationBuilder.RenameColumn(
                name: "ReputationReward",
                table: "Sites",
                newName: "EnteringAssaults");

            migrationBuilder.AlterColumn<int>(
                name: "GameBoardId",
                table: "Sites",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Burn",
                table: "Sites",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BurnedByPlayerId",
                table: "Sites",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Sites",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConqueredOnTurn",
                table: "Sites",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Defenders",
                table: "Sites",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HexSpaceId",
                table: "Sites",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InteractConquered",
                table: "Sites",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InteractOptions",
                table: "Sites",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Rewards",
                table: "Sites",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteId",
                table: "Sites",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhenRevealed",
                table: "Sites",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_BurnedByPlayerId",
                table: "Sites",
                column: "BurnedByPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_GameSessionId",
                table: "Sites",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_HexSpaceId",
                table: "Sites",
                column: "HexSpaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_GameBoards_GameBoardId",
                table: "Sites",
                column: "GameBoardId",
                principalTable: "GameBoards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_GamePlayers_BurnedByPlayerId",
                table: "Sites",
                column: "BurnedByPlayerId",
                principalTable: "GamePlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_GameSessions_GameSessionId",
                table: "Sites",
                column: "GameSessionId",
                principalTable: "GameSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_HexSpaces_HexSpaceId",
                table: "Sites",
                column: "HexSpaceId",
                principalTable: "HexSpaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_GameBoards_GameBoardId",
                table: "Sites");

            migrationBuilder.DropForeignKey(
                name: "FK_Sites_GamePlayers_BurnedByPlayerId",
                table: "Sites");

            migrationBuilder.DropForeignKey(
                name: "FK_Sites_GameSessions_GameSessionId",
                table: "Sites");

            migrationBuilder.DropForeignKey(
                name: "FK_Sites_HexSpaces_HexSpaceId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_BurnedByPlayerId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_GameSessionId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_HexSpaceId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Burn",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "BurnedByPlayerId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ConqueredOnTurn",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Defenders",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "HexSpaceId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "InteractConquered",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "InteractOptions",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Rewards",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "WhenRevealed",
                table: "Sites");

            migrationBuilder.RenameColumn(
                name: "IsRevealed",
                table: "Sites",
                newName: "Y");

            migrationBuilder.RenameColumn(
                name: "IsFortified",
                table: "Sites",
                newName: "X");

            migrationBuilder.RenameColumn(
                name: "IsBurned",
                table: "Sites",
                newName: "SiteSubType");

            migrationBuilder.RenameColumn(
                name: "GameSessionId",
                table: "Sites",
                newName: "RequiredLevel");

            migrationBuilder.RenameColumn(
                name: "EnteringAssaults",
                table: "Sites",
                newName: "ReputationReward");

            migrationBuilder.RenameColumn(
                name: "BurnedAt",
                table: "Sites",
                newName: "UnitReward");

            migrationBuilder.AlterColumn<int>(
                name: "GameBoardId",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArtifactReward",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttackCost",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CrystalsReward",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<int>(
                name: "FameReward",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsExplored",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<string>(
                name: "SpecialRequirements",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpellReward",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_GameBoards_GameBoardId",
                table: "Sites",
                column: "GameBoardId",
                principalTable: "GameBoards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
