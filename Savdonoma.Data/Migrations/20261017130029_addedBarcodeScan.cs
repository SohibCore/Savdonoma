using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedBarcodeScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SYSTEM_SETTINGS",
                schema: "sys",
                newName: "SYSTEM_SETTINGS");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_USER_FULL_NAME",
                table: "SYS_USER",
                column: "FULL_NAME");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_USER_USER_NAME",
                table: "SYS_USER",
                column: "USER_NAME");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_SALES_NUMBER",
                table: "SYS_SALES",
                column: "NUMBER");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_SALE_ITEMS_ID",
                table: "SYS_SALE_ITEMS",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_PRODUCT_PRICE",
                table: "SYS_PRODUCT",
                column: "PRICE");

            migrationBuilder.CreateIndex(
                name: "IX_SYSTEM_SETTINGS_INVENTORY_ENABLED",
                table: "SYSTEM_SETTINGS",
                column: "INVENTORY_ENABLED");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SYS_USER_FULL_NAME",
                table: "SYS_USER");

            migrationBuilder.DropIndex(
                name: "IX_SYS_USER_USER_NAME",
                table: "SYS_USER");

            migrationBuilder.DropIndex(
                name: "IX_SYS_SALES_NUMBER",
                table: "SYS_SALES");

            migrationBuilder.DropIndex(
                name: "IX_SYS_SALE_ITEMS_ID",
                table: "SYS_SALE_ITEMS");

            migrationBuilder.DropIndex(
                name: "IX_SYS_PRODUCT_PRICE",
                table: "SYS_PRODUCT");

            migrationBuilder.DropIndex(
                name: "IX_SYSTEM_SETTINGS_INVENTORY_ENABLED",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.EnsureSchema(
                name: "sys");

            migrationBuilder.RenameTable(
                name: "SYSTEM_SETTINGS",
                newName: "SYSTEM_SETTINGS",
                newSchema: "sys");
        }
    }
}
