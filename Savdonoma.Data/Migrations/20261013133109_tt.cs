using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class tt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "QUANTITY",
                table: "SYS_PRODUCT",
                type: "decimal",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "QUANTITY",
                table: "SYS_PRODUCT",
                type: "decimal",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal",
                oldDefaultValue: 0m);
        }
    }
}
