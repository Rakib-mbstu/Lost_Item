using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lost_Item.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexesWithNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_IMEI",
                table: "Products",
                column: "IMEI",
                unique: true,
                filter: "[IMEI] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_FrameNumber",
                table: "Products",
                column: "FrameNumber",
                unique: true,
                filter: "[FrameNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_EngineNumber",
                table: "Products",
                column: "EngineNumber",
                unique: true,
                filter: "[EngineNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SerialNumber",
                table: "Products",
                column: "SerialNumber",
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_MacAddress",
                table: "Products",
                column: "MacAddress",
                unique: true,
                filter: "[MacAddress] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Products_IMEI",         table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_FrameNumber",   table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_EngineNumber",  table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_SerialNumber",  table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_MacAddress",    table: "Products");
        }
    }
}
