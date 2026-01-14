using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIS.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncDateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SyncDate",
                table: "EmployeeProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncDate",
                table: "EmployeeProfiles");
        }
    }
}
