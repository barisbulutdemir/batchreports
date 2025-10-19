using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace takip.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldProductionJsonToShiftRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MoldProductionJson",
                table: "ShiftRecords",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoldProductionJson",
                table: "ShiftRecords");
        }
    }
}
