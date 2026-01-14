using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CIS.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Divisions",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "ส่วนช่วยอำนวยการ" },
                    { 2, "ส่วนคลัง" },
                    { 3, "ส่วนจัดการงานคดี" },
                    { 4, "ส่วนช่วยพิจารณาคดี" },
                    { 5, "ส่วนบริการประชาชนและประชาสัมพันธ์" },
                    { 6, "ส่วนไกล่เกลี่ยและประนอมข้อพิพาทฯ" },
                    { 7, "ส่วนเทคโนโลยีสารสนเทศ" },
                    { 8, "ส่วนบังคับคดีผู้ประกัน" },
                    { 9, "ส่วนเจ้าพนักงานตำรวจศาล" },
                    { 10, "ผู้บริหาร" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Divisions");
        }
    }
}
