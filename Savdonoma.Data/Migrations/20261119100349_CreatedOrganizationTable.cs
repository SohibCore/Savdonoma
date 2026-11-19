using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatedOrganizationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "USER_ROLE",
                table: "SYS_USER",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 12)
                .OldAnnotation("Relational:ColumnOrder", 11);

            migrationBuilder.AlterColumn<bool>(
                name: "SELF_REGISTER",
                table: "SYS_USER",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean")
                .Annotation("Relational:ColumnOrder", 8)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<bool>(
                name: "IS_ACTIVE",
                table: "SYS_USER",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<string>(
                name: "INN",
                table: "SYS_USER",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NULL",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "NULL")
                .Annotation("Relational:ColumnOrder", 10)
                .OldAnnotation("Relational:ColumnOrder", 9);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 11)
                .OldAnnotation("Relational:ColumnOrder", 10);

            migrationBuilder.AlterColumn<string>(
                name: "ADDRESS",
                table: "SYS_USER",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "NULL",
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldDefaultValue: "NULL")
                .Annotation("Relational:ColumnOrder", 9)
                .OldAnnotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<int>(
                name: "ORGANIZATION_ID",
                table: "SYS_USER",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.CreateTable(
                name: "SYS_ORGANIZATION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NAME = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ADDRESS = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    INN = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                    PHONE = table.Column<string>(type: "varchar(12)", nullable: false),
                    WEBSITE = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    OWNER_ID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_ORGANIZATION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SYS_ORGANIZATION_SYS_USER_OWNER_ID",
                        column: x => x.OWNER_ID,
                        principalTable: "SYS_USER",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SYS_ORGANIZATION_OWNER_ID",
                table: "SYS_ORGANIZATION",
                column: "OWNER_ID");

            migrationBuilder.Sql(@"
INSERT INTO SYS_ORGANIZATION (NAME, ADDRESS, INN, PHONE, WEBSITE, OWNER_ID)
SELECT ORG_NAME, COALESCE(ADDRESS, ''), COALESCE(INN, ''), '', '', ID
FROM SYS_USER
WHERE USER_ROLE = 'Boshliq';

UPDATE SYS_USER
SET ORGANIZATION_ID = (
    SELECT ID FROM SYS_ORGANIZATION WHERE OWNER_ID = SYS_USER.ID
)
WHERE USER_ROLE = 'Boshliq';

UPDATE SYS_USER
SET ORGANIZATION_ID = (
    SELECT shop.ID
    FROM SYS_ORGANIZATION shop
    JOIN SYS_USER shop_owner ON shop_owner.ID = shop.OWNER_ID
    WHERE shop_owner.ORG_NAME = SYS_USER.ORG_NAME
      AND shop_owner.USER_ROLE = 'Boshliq'
    ORDER BY shop_owner.ID
    LIMIT 1
)
WHERE USER_ROLE <> 'Boshliq'
  AND EXISTS (
    SELECT 1
    FROM SYS_ORGANIZATION shop
    JOIN SYS_USER shop_owner ON shop_owner.ID = shop.OWNER_ID
    WHERE shop_owner.ORG_NAME = SYS_USER.ORG_NAME
      AND shop_owner.USER_ROLE = 'Boshliq'
  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SYS_ORGANIZATION");

            migrationBuilder.DropColumn(
                name: "ORGANIZATION_ID",
                table: "SYS_USER");

            migrationBuilder.AlterColumn<string>(
                name: "USER_ROLE",
                table: "SYS_USER",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 11)
                .OldAnnotation("Relational:ColumnOrder", 12);

            migrationBuilder.AlterColumn<bool>(
                name: "SELF_REGISTER",
                table: "SYS_USER",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean")
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 8);

            migrationBuilder.AlterColumn<bool>(
                name: "IS_ACTIVE",
                table: "SYS_USER",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<string>(
                name: "INN",
                table: "SYS_USER",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NULL",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "NULL")
                .Annotation("Relational:ColumnOrder", 9)
                .OldAnnotation("Relational:ColumnOrder", 10);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 10)
                .OldAnnotation("Relational:ColumnOrder", 11);

            migrationBuilder.AlterColumn<string>(
                name: "ADDRESS",
                table: "SYS_USER",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "NULL",
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldDefaultValue: "NULL")
                .Annotation("Relational:ColumnOrder", 8)
                .OldAnnotation("Relational:ColumnOrder", 9);
        }
    }
}
