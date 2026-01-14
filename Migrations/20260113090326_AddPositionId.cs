using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIS.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "EmployeeProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "EmployeeProfiles");
        }
    }
}
