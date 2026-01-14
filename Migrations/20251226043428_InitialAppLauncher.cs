using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIS.Migrations
{
    /// <inheritdoc />
    public partial class InitialAppLauncher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LauncherGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LauncherGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LauncherLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    LauncherGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LauncherLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LauncherLinks_LauncherGroups_LauncherGroupId",
                        column: x => x.LauncherGroupId,
                        principalTable: "LauncherGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LauncherLinks_LauncherGroupId",
                table: "LauncherLinks",
                column: "LauncherGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LauncherLinks");

            migrationBuilder.DropTable(
                name: "LauncherGroups");
        }
    }
}
