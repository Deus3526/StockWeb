using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewStock.EFModels.Migrations
{
    /// <inheritdoc />
    public partial class StockDayInfoCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockDayInfo",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StockId = table.Column<short>(type: "smallint", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    漲幅 = table.Column<double>(type: "float", nullable: false),
                    收盤價 = table.Column<double>(type: "float", nullable: false),
                    開盤價 = table.Column<double>(type: "float", nullable: false),
                    最高價 = table.Column<double>(type: "float", nullable: false),
                    最低價 = table.Column<double>(type: "float", nullable: false),
                    平盤價 = table.Column<double>(type: "float", nullable: false),
                    成交量 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDayInfo", x => new { x.StockId, x.Date });
                    table.ForeignKey(
                        name: "FK_StockDayInfo_StockInfo_StockId",
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
                name: "StockDayInfo");
        }
    }
}
