using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewStock.EFModels.Migrations
{
    /// <inheritdoc />
    public partial class 周月K資料表 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "周月K資料表",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StockId = table.Column<short>(type: "smallint", nullable: false),
                    TimeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    交易筆數 = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_周月K資料表", x => new { x.StockId, x.Date, x.TimeType });
                    table.ForeignKey(
                        name: "FK_周月K資料表_StockInfo_StockId",
                        column: x => x.StockId,
                        principalTable: "StockInfo",
                        principalColumn: "StockId",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "周月K資料表");
        }
    }
}
