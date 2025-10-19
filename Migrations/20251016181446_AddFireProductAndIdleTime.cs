using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace takip.Migrations
{
    /// <inheritdoc />
    public partial class AddFireProductAndIdleTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MoldProductionJson",
                table: "ShiftRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "IdleTimeSeconds",
                table: "ShiftRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Mixer1MaterialsJson",
                table: "ShiftRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mixer2MaterialsJson",
                table: "ShiftRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TotalMaterialsJson",
                table: "ShiftRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ConcreteBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ConcreteBatch2s",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActiveShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShiftRecordId = table.Column<int>(type: "integer", nullable: false),
                    ProductionStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartTotalProduction = table.Column<int>(type: "integer", nullable: false),
                    StartDm452Value = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartFireProductCount = table.Column<int>(type: "integer", nullable: false),
                    IdleTimeSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveShifts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveShift_IsActive",
                table: "ActiveShifts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveShift_OperatorName",
                table: "ActiveShifts",
                column: "OperatorName");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveShift_ShiftStartTime",
                table: "ActiveShifts",
                column: "ShiftStartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveShifts");

            migrationBuilder.DropColumn(
                name: "IdleTimeSeconds",
                table: "ShiftRecords");

            migrationBuilder.DropColumn(
                name: "Mixer1MaterialsJson",
                table: "ShiftRecords");

            migrationBuilder.DropColumn(
                name: "Mixer2MaterialsJson",
                table: "ShiftRecords");

            migrationBuilder.DropColumn(
                name: "TotalMaterialsJson",
                table: "ShiftRecords");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ConcreteBatches");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ConcreteBatch2s");

            migrationBuilder.AlterColumn<string>(
                name: "MoldProductionJson",
                table: "ShiftRecords",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
