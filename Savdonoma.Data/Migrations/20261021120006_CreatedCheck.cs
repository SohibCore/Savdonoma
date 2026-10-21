using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatedCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ADDRESS",
                table: "SYSTEM_SETTINGS",
                type: "text",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 4);

            migrationBuilder.AddColumn<int>(
                name: "CHECK_LENGTH",
                table: "SYSTEM_SETTINGS",
                type: "integer",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 6);

            migrationBuilder.AddColumn<string>(
                name: "ORG_NAME",
                table: "SYSTEM_SETTINGS",
                type: "text",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddColumn<string>(
                name: "PRINTER_NAME",
                table: "SYSTEM_SETTINGS",
                type: "text",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYS_SALES",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "USER_ID",
                table: "SYS_SALES",
                type: "integer",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 11);

            migrationBuilder.CreateIndex(
                name: "IX_SYS_SALES_USER_ID",
                table: "SYS_SALES",
                column: "USER_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_SYS_SALES_SYS_USER_USER_ID",
                table: "SYS_SALES",
                column: "USER_ID",
                principalTable: "SYS_USER",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SYS_SALES_SYS_USER_USER_ID",
                table: "SYS_SALES");

            migrationBuilder.DropIndex(
                name: "IX_SYS_SALES_USER_ID",
                table: "SYS_SALES");

            migrationBuilder.DropColumn(
                name: "ADDRESS",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.DropColumn(
                name: "CHECK_LENGTH",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.DropColumn(
                name: "ORG_NAME",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.DropColumn(
                name: "PRINTER_NAME",
                table: "SYSTEM_SETTINGS");

            migrationBuilder.DropColumn(
                name: "USER_ID",
                table: "SYS_SALES");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYS_SALES",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
