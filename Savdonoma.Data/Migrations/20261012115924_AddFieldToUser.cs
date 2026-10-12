using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PASSWORD",
                table: "SYS_USER",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6)");

            migrationBuilder.AddColumn<bool>(
                name: "IS_ACTIVE",
                table: "SYS_USER",
                type: "boolean",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.AddColumn<bool>(
                name: "SELF_REGISTER",
                table: "SYS_USER",
                type: "boolean",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 7);

            migrationBuilder.AddColumn<string>(
                name: "USER_ROLE",
                table: "SYS_USER",
                type: "text",
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IS_ACTIVE",
                table: "SYS_USER");

            migrationBuilder.DropColumn(
                name: "SELF_REGISTER",
                table: "SYS_USER");

            migrationBuilder.DropColumn(
                name: "USER_ROLE",
                table: "SYS_USER");

            migrationBuilder.AlterColumn<string>(
                name: "PASSWORD",
                table: "SYS_USER",
                type: "varchar(6)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500);
        }
    }
}
