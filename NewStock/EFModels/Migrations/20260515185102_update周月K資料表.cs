using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewStock.EFModels.Migrations
{
    /// <inheritdoc />
    public partial class update周月K資料表 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "周月K資料表",
                newName: "開盤價");

            migrationBuilder.AddColumn<double>(
                name: "平盤價",
                table: "周月K資料表",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "收盤價",
                table: "周月K資料表",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "最低價",
                table: "周月K資料表",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "最高價",
                table: "周月K資料表",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "漲幅",
                table: "周月K資料表",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "平盤價",
                table: "周月K資料表");

            migrationBuilder.DropColumn(
                name: "收盤價",
                table: "周月K資料表");

            migrationBuilder.DropColumn(
                name: "最低價",
                table: "周月K資料表");

            migrationBuilder.DropColumn(
                name: "最高價",
                table: "周月K資料表");

            migrationBuilder.DropColumn(
                name: "漲幅",
                table: "周月K資料表");

            migrationBuilder.RenameColumn(
                name: "開盤價",
                table: "周月K資料表",
                newName: "Value");
        }
    }
}
