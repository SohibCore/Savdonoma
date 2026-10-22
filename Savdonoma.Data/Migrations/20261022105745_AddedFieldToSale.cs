using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedFieldToSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CHANGE_AMOUNT",
                table: "SYS_SALES",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 13);

            migrationBuilder.AddColumn<decimal>(
                name: "RECEIVED_AMOUNT",
                table: "SYS_SALES",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 12);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CHANGE_AMOUNT",
                table: "SYS_SALES");

            migrationBuilder.DropColumn(
                name: "RECEIVED_AMOUNT",
                table: "SYS_SALES");
        }
    }
}
