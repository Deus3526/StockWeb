using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewStock.EFModels.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockInfo",
                columns: table => new
                {
                    StockId = table.Column<short>(type: "smallint", nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IndustryCategory = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MarketType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockInfo", x => x.StockId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockInfo");
        }
    }
}
