using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIS.Migrations
{
    /// <inheritdoc />
    public partial class AddColorToLauncher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorClass",
                table: "LauncherLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorClass",
                table: "LauncherLinks");
        }
    }
}
