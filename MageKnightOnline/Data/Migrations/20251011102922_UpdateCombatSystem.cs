using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MageKnightOnline.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCombatSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LootTable",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "SpecialAbilities",
                table: "Enemies");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Enemies",
                newName: "IsSiege");

            migrationBuilder.RenameColumn(
                name: "BlockValue",
                table: "Enemies",
                newName: "IsRanged");

            migrationBuilder.RenameColumn(
                name: "AttackValue",
                table: "Enemies",
                newName: "IsFortified");

            migrationBuilder.AddColumn<string>(
                name: "Abilities",
                table: "Enemies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Armor",
                table: "Enemies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Attack",
                table: "Enemies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Color",
                table: "Enemies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FameValue",
                table: "Enemies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsElite",
                table: "Enemies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Resistances",
                table: "Enemies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "SiteId",
                table: "Combats",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "AttackingPlayerId",
                table: "Combats",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CombatState",
                table: "Combats",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CurrentParticipantId1",
                table: "Combats",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentPhase",
                table: "Combats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefendingPlayerId",
                table: "Combats",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAttackPhase",
                table: "Combats",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlockPhase",
                table: "Combats",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInitiativePhase",
                table: "Combats",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolutionPhase",
                table: "Combats",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Modifiers",
                table: "Combats",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TurnNumber",
                table: "Combats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttackModifier",
                table: "CombatParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlockModifier",
                table: "CombatParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CombatOrder",
                table: "CombatParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DamageModifier",
                table: "CombatParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnemyId1",
                table: "CombatParticipants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CombatParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Resistances",
                table: "CombatParticipants",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecialAbilities",
                table: "CombatParticipants",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

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
                name: "DamageDealt",
                table: "CombatActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DamageReceived",
                table: "CombatActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "CombatActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Modifiers",
                table: "CombatActions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CombatActions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "CombatActions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecialEffects",
                table: "CombatActions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TurnNumber",
                table: "CombatActions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Combats_AttackingPlayerId",
                table: "Combats",
                column: "AttackingPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Combats_CurrentParticipantId1",
                table: "Combats",
                column: "CurrentParticipantId1");

            migrationBuilder.CreateIndex(
                name: "IX_Combats_DefendingPlayerId",
                table: "Combats",
                column: "DefendingPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatParticipants_EnemyId1",
                table: "CombatParticipants",
                column: "EnemyId1");

            migrationBuilder.CreateIndex(
                name: "IX_CombatActions_TargetId",
                table: "CombatActions",
                column: "TargetId");

            migrationBuilder.AddForeignKey(
                name: "FK_CombatActions_CombatParticipants_TargetId",
                table: "CombatActions",
                column: "TargetId",
                principalTable: "CombatParticipants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CombatParticipants_Enemies_EnemyId1",
                table: "CombatParticipants",
                column: "EnemyId1",
                principalTable: "Enemies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Combats_CombatParticipants_CurrentParticipantId1",
                table: "Combats",
                column: "CurrentParticipantId1",
                principalTable: "CombatParticipants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Combats_GamePlayers_AttackingPlayerId",
                table: "Combats",
                column: "AttackingPlayerId",
                principalTable: "GamePlayers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Combats_GamePlayers_DefendingPlayerId",
                table: "Combats",
                column: "DefendingPlayerId",
                principalTable: "GamePlayers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CombatActions_CombatParticipants_TargetId",
                table: "CombatActions");

            migrationBuilder.DropForeignKey(
                name: "FK_CombatParticipants_Enemies_EnemyId1",
                table: "CombatParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Combats_CombatParticipants_CurrentParticipantId1",
                table: "Combats");

            migrationBuilder.DropForeignKey(
                name: "FK_Combats_GamePlayers_AttackingPlayerId",
                table: "Combats");

            migrationBuilder.DropForeignKey(
                name: "FK_Combats_GamePlayers_DefendingPlayerId",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_Combats_AttackingPlayerId",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_Combats_CurrentParticipantId1",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_Combats_DefendingPlayerId",
                table: "Combats");

            migrationBuilder.DropIndex(
                name: "IX_CombatParticipants_EnemyId1",
                table: "CombatParticipants");

            migrationBuilder.DropIndex(
                name: "IX_CombatActions_TargetId",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "Abilities",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "Armor",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "Attack",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "FameValue",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "IsElite",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "Resistances",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "AttackingPlayerId",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "CombatState",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "CurrentParticipantId1",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "CurrentPhase",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "DefendingPlayerId",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "IsAttackPhase",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "IsBlockPhase",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "IsInitiativePhase",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "IsResolutionPhase",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "Modifiers",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "TurnNumber",
                table: "Combats");

            migrationBuilder.DropColumn(
                name: "AttackModifier",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "BlockModifier",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "CombatOrder",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "DamageModifier",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "EnemyId1",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "Resistances",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "SpecialAbilities",
                table: "CombatParticipants");

            migrationBuilder.DropColumn(
                name: "ActionSequence",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "DamageDealt",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "DamageReceived",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "Modifiers",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "SpecialEffects",
                table: "CombatActions");

            migrationBuilder.DropColumn(
                name: "TurnNumber",
                table: "CombatActions");

            migrationBuilder.RenameColumn(
                name: "IsSiege",
                table: "Enemies",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "IsRanged",
                table: "Enemies",
                newName: "BlockValue");

            migrationBuilder.RenameColumn(
                name: "IsFortified",
                table: "Enemies",
                newName: "AttackValue");

            migrationBuilder.AddColumn<string>(
                name: "LootTable",
                table: "Enemies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialAbilities",
                table: "Enemies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SiteId",
                table: "Combats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CombatActions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
