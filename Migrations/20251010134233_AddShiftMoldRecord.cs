using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace takip.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftMoldRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MoldProductions");

            migrationBuilder.DropColumn(
                name: "MoldProductionJson",
                table: "ShiftRecords");

            migrationBuilder.CreateTable(
                name: "ShiftMoldRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    MoldId = table.Column<int>(type: "integer", nullable: false),
                    MoldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartProductionCount = table.Column<int>(type: "integer", nullable: false),
                    EndProductionCount = table.Column<int>(type: "integer", nullable: false),
                    ProductionCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftMoldRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftMoldRecords");

            migrationBuilder.AddColumn<string>(
                name: "MoldProductionJson",
                table: "ShiftRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MoldProductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MoldId = table.Column<int>(type: "integer", nullable: false),
                    ShiftRecordId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MoldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductionCount = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoldProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoldProductions_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MoldProductions_ShiftRecords_ShiftRecordId",
                        column: x => x.ShiftRecordId,
                        principalTable: "ShiftRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MoldProductions_MoldId",
                table: "MoldProductions",
                column: "MoldId");

            migrationBuilder.CreateIndex(
                name: "IX_MoldProductions_ShiftRecordId",
                table: "MoldProductions",
                column: "ShiftRecordId");
        }
    }
}
