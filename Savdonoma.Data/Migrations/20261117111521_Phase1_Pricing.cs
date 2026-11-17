using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Pricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DEFAULT_MARKUP_PERCENT",
                table: "SYSTEM_SETTINGS",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<int>(
                name: "PRICE_ROUNDING_STEP",
                table: "SYSTEM_SETTINGS",
                type: "integer",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 9);

            migrationBuilder.AddColumn<decimal>(
                name: "MARKUP_PERCENT",
                table: "SYS_PRODUCT",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 18);

            migrationBuilder.AddColumn<decimal>(
                name: "MARKUP_PERCENT",
                table: "SYS_CATEGORY",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DEFAULT_MARKUP_PERCENT",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.DropColumn(
                name: "PRICE_ROUNDING_STEP",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.DropColumn(
                name: "MARKUP_PERCENT",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "MARKUP_PERCENT",
                table: "SYS_CATEGORY");
        }
    }
}
