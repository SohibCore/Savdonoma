using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SYS_CATEGORY",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NAME = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_CATEGORY", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SYS_SALES",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NUMBER = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TOTAL_AMOUNT = table.Column<long>(type: "bigint", nullable: false),
                    PAYMENT_METHOD = table.Column<string>(type: "text", nullable: false),
                    STATUS = table.Column<string>(type: "text", nullable: false),
                    CANCEL_REASON = table.Column<string>(type: "varchar(500)", nullable: true),
                    CANCELLED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_SALES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SYS_PRODUCT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UNIT = table.Column<string>(type: "text", nullable: false),
                    PRICE = table.Column<long>(type: "bigint", nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false),
                    CATEGORY_ID = table.Column<int>(type: "integer", nullable: false),
                    BARCODE = table.Column<string>(type: "text", nullable: true),
                    NAME = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    NAME_SEARCH = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_PRODUCT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_PRODUCT_SYS_CATEGORY_CATEGORY_ID",
                        column: x => x.CATEGORY_ID,
                        principalTable: "SYS_CATEGORY",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SYS_SALE_ITEMS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SALE_ID = table.Column<int>(type: "integer", nullable: false),
                    PRODUCT_ID = table.Column<int>(type: "integer", nullable: false),
                    QUANTITY = table.Column<decimal>(type: "integer", nullable: false),
                    UNIT_PRICE = table.Column<long>(type: "bigint", nullable: false),
                    PRODUCT_NAME = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    LINE_TOTAL = table.Column<long>(type: "bigint", nullable: false),
                    TOTAL_AMOUNT = table.Column<long>(type: "bigint", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_SALE_ITEMS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_SALE_ITEMS_SYS_PRODUCT_PRODUCT_ID",
                        column: x => x.PRODUCT_ID,
                        principalTable: "SYS_PRODUCT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SYS_SALE_ITEMS_SYS_SALES_SALE_ID",
                        column: x => x.SALE_ID,
                        principalTable: "SYS_SALES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SYS_PRODUCT_BARCODE",
                table: "SYS_PRODUCT",
                column: "BARCODE",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SYS_PRODUCT_CATEGORY_ID",
                table: "SYS_PRODUCT",
                column: "CATEGORY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_PRODUCT_NAME_SEARCH",
                table: "SYS_PRODUCT",
                column: "NAME_SEARCH");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_SALE_ITEMS_PRODUCT_ID",
                table: "SYS_SALE_ITEMS",
                column: "PRODUCT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_SALE_ITEMS_SALE_ID",
                table: "SYS_SALE_ITEMS",
                column: "SALE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SYS_SALE_ITEMS");

            migrationBuilder.DropTable(
                name: "SYS_PRODUCT");

            migrationBuilder.DropTable(
                name: "SYS_SALES");

            migrationBuilder.DropTable(
                name: "SYS_CATEGORY");
        }
    }
}
