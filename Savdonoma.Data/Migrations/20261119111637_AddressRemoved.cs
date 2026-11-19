using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddressRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ADDRESS",
                table: "SYS_USER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ADDRESS",
                table: "SYS_USER",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "NULL")
                .Annotation("Relational:ColumnOrder", 9);

            migrationBuilder.Sql(@"
UPDATE SYS_USER
SET ADDRESS = COALESCE((
    SELECT ADDRESS FROM SYS_ORGANIZATION
    WHERE SYS_ORGANIZATION.ID = SYS_USER.ORGANIZATION_ID
), 'NULL');");
        }
    }
}
