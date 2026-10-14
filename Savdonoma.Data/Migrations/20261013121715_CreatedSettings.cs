using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatedSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sys");

            migrationBuilder.AddColumn<decimal>(
                name: "QUANTITY",
                table: "SYS_PRODUCT",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.CreateTable(
                name: "SYSTEM_SETTINGS",
                schema: "sys",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    INVENTORY_ENABLED = table.Column<bool>(type: "boolean", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYSTEM_SETTINGS", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SYSTEM_SETTINGS",
                schema: "sys");

            migrationBuilder.DropColumn(
                name: "QUANTITY",
                table: "SYS_PRODUCT");
        }
    }
}
