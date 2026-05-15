using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewStock.EFModels.Migrations
{
    /// <inheritdoc />
    public partial class update分k資料表 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "平盤價",
                table: "分K資料表");

            migrationBuilder.DropColumn(
                name: "漲幅",
                table: "分K資料表");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "平盤價",
                table: "分K資料表",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "漲幅",
                table: "分K資料表",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
