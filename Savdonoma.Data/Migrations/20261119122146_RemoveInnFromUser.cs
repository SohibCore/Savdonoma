using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInnFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "INN",
                table: "SYS_USER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 10)
                .OldAnnotation("Relational:ColumnOrder", 12);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 9)
                .OldAnnotation("Relational:ColumnOrder", 11);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UPDATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 12)
                .OldAnnotation("Relational:ColumnOrder", 10);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_AT",
                table: "SYS_USER",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 11)
                .OldAnnotation("Relational:ColumnOrder", 9);

            migrationBuilder.AddColumn<string>(
                name: "INN",
                table: "SYS_USER",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NULL")
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.Sql(@"
UPDATE SYS_USER
SET INN = COALESCE((
    SELECT INN FROM SYS_ORGANIZATION
    WHERE SYS_ORGANIZATION.ID = SYS_USER.ORGANIZATION_ID
), 'NULL');");
        }
    }
}
