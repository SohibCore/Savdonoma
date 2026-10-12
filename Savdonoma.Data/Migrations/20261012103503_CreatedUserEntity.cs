using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatedUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SYS_USER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FULL_NAME = table.Column<string>(type: "varchar(350)", maxLength: 350, nullable: false),
                    USER_NAME = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    PASSWORD = table.Column<string>(type: "varchar(6)", nullable: false),
                    ORG_NAME = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_USER", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SYS_USER");
        }
    }
}
