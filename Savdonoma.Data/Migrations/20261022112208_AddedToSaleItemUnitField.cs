using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedToSaleItemUnitField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UNIT",
                table: "SYS_SALE_ITEMS",
                type: "text",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.Sql(
                """
                UPDATE SYS_SALE_ITEMS
                SET UNIT = (
                    SELECT UNIT
                    FROM SYS_PRODUCT
                    WHERE SYS_PRODUCT.ID = SYS_SALE_ITEMS.PRODUCT_ID
                )
                WHERE UNIT = ''
                  AND EXISTS (
                    SELECT 1
                    FROM SYS_PRODUCT
                    WHERE SYS_PRODUCT.ID = SYS_SALE_ITEMS.PRODUCT_ID
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UNIT",
                table: "SYS_SALE_ITEMS");
        }
    }
}
