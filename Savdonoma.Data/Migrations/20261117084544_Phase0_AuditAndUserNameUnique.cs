using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase0_AuditAndUserNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SYS_USER_USER_NAME",
                table: "SYS_USER");

            migrationBuilder.AlterColumn<string>(
                name: "OLD_VALUE",
                table: "SYS_AUDIT_LOGS",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NEW_VALUE",
                table: "SYS_AUDIT_LOGS",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ENTITY_NAME",
                table: "SYS_AUDIT_LOGS",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "ENTITY_ID",
                table: "SYS_AUDIT_LOGS",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 6);

            // Unique indeksdan oldin: takror loginlar bo'lsa, birinchisidan boshqalariga "_ID" qo'shiladi
            migrationBuilder.Sql(
                "UPDATE SYS_USER SET USER_NAME = USER_NAME || '_' || ID " +
                "WHERE ID NOT IN (SELECT MIN(ID) FROM SYS_USER GROUP BY USER_NAME);");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_USER_USER_NAME",
                table: "SYS_USER",
                column: "USER_NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SYS_AUDIT_LOGS_ENTITY_NAME_ENTITY_ID",
                table: "SYS_AUDIT_LOGS",
                columns: new[] { "ENTITY_NAME", "ENTITY_ID" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SYS_USER_USER_NAME",
                table: "SYS_USER");

            migrationBuilder.DropIndex(
                name: "IX_SYS_AUDIT_LOGS_ENTITY_NAME_ENTITY_ID",
                table: "SYS_AUDIT_LOGS");

            migrationBuilder.DropColumn(
                name: "ENTITY_ID",
                table: "SYS_AUDIT_LOGS");

            migrationBuilder.AlterColumn<string>(
                name: "OLD_VALUE",
                table: "SYS_AUDIT_LOGS",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NEW_VALUE",
                table: "SYS_AUDIT_LOGS",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ENTITY_NAME",
                table: "SYS_AUDIT_LOGS",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_SYS_USER_USER_NAME",
                table: "SYS_USER",
                column: "USER_NAME");
        }
    }
}
