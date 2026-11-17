using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_StockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ORG_NAME",
                table: "SYSTEM_SETTINGS",
                newName: "SHOP_NAME");

            migrationBuilder.RenameColumn(
                name: "SaleStatus",
                table: "SYS_SALES",
                newName: "SALE_STATUS");

            migrationBuilder.RenameColumn(
                name: "TOTAL_AMOUNT",
                table: "SYS_SALE_ITEMS",
                newName: "COST_PRICE");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "SYS_CATEGORY",
                newName: "UPDATED_AT");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "SYS_CATEGORY",
                newName: "CREATED_AT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SYSTEM_SETTINGS",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYSTEM_SETTINGS",
                type: "timestamp with time zone",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "SALE_STATUS",
                table: "SYS_SALES",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Relational:ColumnOrder", 14);

            migrationBuilder.AddColumn<long>(
                name: "COST_PRICE",
                table: "SYS_PRODUCT",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Relational:ColumnOrder", 11);

            migrationBuilder.AddColumn<bool>(
                name: "IS_MARKED",
                table: "SYS_PRODUCT",
                type: "boolean",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 17);

            migrationBuilder.AddColumn<decimal>(
                name: "MIN_QUANTITY",
                table: "SYS_PRODUCT",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 12);

            migrationBuilder.AddColumn<string>(
                name: "MXIK_CODE",
                table: "SYS_PRODUCT",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 14);

            migrationBuilder.AddColumn<string>(
                name: "PACKAGE_CODE",
                table: "SYS_PRODUCT",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 15);

            migrationBuilder.AddColumn<string>(
                name: "SCALE_CODE",
                table: "SYS_PRODUCT",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 13);

            migrationBuilder.AddColumn<decimal>(
                name: "VAT_PERCENT",
                table: "SYS_PRODUCT",
                type: "decimal",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 16);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SYS_CATEGORY",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYS_CATEGORY",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.CreateTable(
                name: "SYS_PRODUCT_BARCODE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PRODUCT_ID = table.Column<int>(type: "integer", nullable: false),
                    BARCODE = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_PRODUCT_BARCODE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_PRODUCT_BARCODE_SYS_PRODUCT_PRODUCT_ID",
                        column: x => x.PRODUCT_ID,
                        principalTable: "SYS_PRODUCT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SYS_SUPPLIER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NAME = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    PHONE = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    INN = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    NOTE = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false),
                    DEBT = table.Column<long>(type: "bigint", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_SUPPLIER", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SYS_STOCK_DOCUMENT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NUMBER = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    TYPE = table.Column<string>(type: "text", nullable: false),
                    STATUS = table.Column<string>(type: "text", nullable: false),
                    SUPPLIER_ID = table.Column<int>(type: "integer", nullable: true),
                    NOTE = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    TOTAL_COST = table.Column<long>(type: "bigint", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CREATED_BY = table.Column<int>(type: "integer", nullable: true),
                    POSTED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    POSTED_BY = table.Column<int>(type: "integer", nullable: true),
                    CANCELLED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CANCELLED_BY = table.Column<int>(type: "integer", nullable: true),
                    CANCEL_REASON = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_STOCK_DOCUMENT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_STOCK_DOCUMENT_SYS_SUPPLIER_SUPPLIER_ID",
                        column: x => x.SUPPLIER_ID,
                        principalTable: "SYS_SUPPLIER",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SYS_SUPPLIER_PAYMENT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SUPPLIER_ID = table.Column<int>(type: "integer", nullable: false),
                    AMOUNT = table.Column<long>(type: "bigint", nullable: false),
                    NOTE = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    USER_ID = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_SUPPLIER_PAYMENT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_SUPPLIER_PAYMENT_SYS_SUPPLIER_SUPPLIER_ID",
                        column: x => x.SUPPLIER_ID,
                        principalTable: "SYS_SUPPLIER",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SYS_STOCK_DOCUMENT_LINE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DOCUMENT_ID = table.Column<int>(type: "integer", nullable: false),
                    PRODUCT_ID = table.Column<int>(type: "integer", nullable: false),
                    QUANTITY = table.Column<decimal>(type: "decimal", nullable: false),
                    COST_PRICE = table.Column<long>(type: "bigint", nullable: false),
                    NEW_SALE_PRICE = table.Column<long>(type: "bigint", nullable: true),
                    EXPECTED_QUANTITY = table.Column<decimal>(type: "decimal", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_STOCK_DOCUMENT_LINE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_STOCK_DOCUMENT_LINE_SYS_PRODUCT_PRODUCT_ID",
                        column: x => x.PRODUCT_ID,
                        principalTable: "SYS_PRODUCT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SYS_STOCK_DOCUMENT_LINE_SYS_STOCK_DOCUMENT_DOCUMENT_ID",
                        column: x => x.DOCUMENT_ID,
                        principalTable: "SYS_STOCK_DOCUMENT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SYS_STOCK_MOVEMENT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PRODUCT_ID = table.Column<int>(type: "integer", nullable: false),
                    TYPE = table.Column<string>(type: "text", nullable: false),
                    QUANTITY = table.Column<decimal>(type: "decimal", nullable: false),
                    BALANCE_AFTER = table.Column<decimal>(type: "decimal", nullable: false),
                    COST_PRICE = table.Column<long>(type: "bigint", nullable: false),
                    DOCUMENT_ID = table.Column<int>(type: "integer", nullable: true),
                    SALE_ID = table.Column<int>(type: "integer", nullable: true),
                    USER_ID = table.Column<int>(type: "integer", nullable: true),
                    NOTE = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_STOCK_MOVEMENT", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_STOCK_MOVEMENT_SYS_PRODUCT_PRODUCT_ID",
                        column: x => x.PRODUCT_ID,
                        principalTable: "SYS_PRODUCT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SYS_STOCK_MOVEMENT_SYS_SALES_SALE_ID",
                        column: x => x.SALE_ID,
                        principalTable: "SYS_SALES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SYS_STOCK_MOVEMENT_SYS_STOCK_DOCUMENT_DOCUMENT_ID",
                        column: x => x.DOCUMENT_ID,
                        principalTable: "SYS_STOCK_DOCUMENT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SYS_PRODUCT_SCALE_CODE",
                table: "SYS_PRODUCT",
                column: "SCALE_CODE",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SYS_PRODUCT_BARCODE_BARCODE",
                table: "SYS_PRODUCT_BARCODE",
                column: "BARCODE",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SYS_PRODUCT_BARCODE_PRODUCT_ID",
                table: "SYS_PRODUCT_BARCODE",
                column: "PRODUCT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_DOCUMENT_NUMBER",
                table: "SYS_STOCK_DOCUMENT",
                column: "NUMBER",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_DOCUMENT_SUPPLIER_ID",
                table: "SYS_STOCK_DOCUMENT",
                column: "SUPPLIER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_DOCUMENT_TYPE_CREATED_AT",
                table: "SYS_STOCK_DOCUMENT",
                columns: new[] { "TYPE", "CREATED_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_DOCUMENT_LINE_DOCUMENT_ID",
                table: "SYS_STOCK_DOCUMENT_LINE",
                column: "DOCUMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_DOCUMENT_LINE_PRODUCT_ID",
                table: "SYS_STOCK_DOCUMENT_LINE",
                column: "PRODUCT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_MOVEMENT_CREATED_AT",
                table: "SYS_STOCK_MOVEMENT",
                column: "CREATED_AT");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_MOVEMENT_DOCUMENT_ID",
                table: "SYS_STOCK_MOVEMENT",
                column: "DOCUMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_MOVEMENT_PRODUCT_ID_ID",
                table: "SYS_STOCK_MOVEMENT",
                columns: new[] { "PRODUCT_ID", "ID" });

            migrationBuilder.CreateIndex(
                name: "IX_SYS_STOCK_MOVEMENT_SALE_ID",
                table: "SYS_STOCK_MOVEMENT",
                column: "SALE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_SUPPLIER_NAME",
                table: "SYS_SUPPLIER",
                column: "NAME");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_SUPPLIER_PAYMENT_SUPPLIER_ID_CREATED_AT",
                table: "SYS_SUPPLIER_PAYMENT",
                columns: new[] { "SUPPLIER_ID", "CREATED_AT" });

            // ---- Ma'lumotlarni ko'chirish ----

            // Eski sxemada "yaratilgan sana" UPDATED_AT ustunida saqlangan edi
            migrationBuilder.Sql(
                "UPDATE SYSTEM_SETTINGS SET CREATED_AT = UPDATED_AT, UPDATED_AT = NULL WHERE CREATED_AT IS NULL;");

            // TOTAL_AMOUNT hech qachon to'ldirilmagan (0 edi); tannarx noma'lum = 0
            migrationBuilder.Sql("UPDATE SYS_SALE_ITEMS SET COST_PRICE = 0;");

            // Hozirgi qoldiqlar jurnalga "Boshlang'ich qoldiq" sifatida yoziladi:
            // shundan keyin har bir mahsulot uchun Quantity = harakatlar yig'indisi
            migrationBuilder.Sql(
                "INSERT INTO SYS_STOCK_MOVEMENT (PRODUCT_ID, TYPE, QUANTITY, BALANCE_AFTER, COST_PRICE, NOTE, CREATED_AT) " +
                "SELECT ID, 'OpeningBalance', QUANTITY, QUANTITY, 0, 'Migratsiya: boshlang''ich qoldiq', " +
                "strftime('%Y-%m-%d %H:%M:%f', 'now') " +
                "FROM SYS_PRODUCT WHERE QUANTITY <> 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SYS_PRODUCT_BARCODE");

            migrationBuilder.DropTable(
                name: "SYS_STOCK_DOCUMENT_LINE");

            migrationBuilder.DropTable(
                name: "SYS_STOCK_MOVEMENT");

            migrationBuilder.DropTable(
                name: "SYS_SUPPLIER_PAYMENT");

            migrationBuilder.DropTable(
                name: "SYS_STOCK_DOCUMENT");

            migrationBuilder.DropTable(
                name: "SYS_SUPPLIER");

            migrationBuilder.DropIndex(
                name: "IX_SYS_PRODUCT_SCALE_CODE",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "CREATED_AT",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.DropColumn(
                name: "COST_PRICE",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "IS_MARKED",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "MIN_QUANTITY",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "MXIK_CODE",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "PACKAGE_CODE",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "SCALE_CODE",
                table: "SYS_PRODUCT");

            migrationBuilder.DropColumn(
                name: "VAT_PERCENT",
                table: "SYS_PRODUCT");

            migrationBuilder.RenameColumn(
                name: "SHOP_NAME",
                table: "SYSTEM_SETTINGS",
                newName: "ORG_NAME");

            migrationBuilder.RenameColumn(
                name: "SALE_STATUS",
                table: "SYS_SALES",
                newName: "SaleStatus");

            migrationBuilder.RenameColumn(
                name: "COST_PRICE",
                table: "SYS_SALE_ITEMS",
                newName: "TOTAL_AMOUNT");

            migrationBuilder.RenameColumn(
                name: "UPDATED_AT",
                table: "SYS_CATEGORY",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CREATED_AT",
                table: "SYS_CATEGORY",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SYSTEM_SETTINGS",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 2)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<int>(
                name: "SaleStatus",
                table: "SYS_SALES",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Relational:ColumnOrder", 14);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "SYS_CATEGORY",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "SYS_CATEGORY",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .OldAnnotation("Relational:ColumnOrder", 3);
        }
    }
}
