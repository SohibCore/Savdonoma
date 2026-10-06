using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Savdonoma.Data.Migrations
{
    /// <inheritdoc />
    public partial class createdAtToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SYS_CATEGORY",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SYS_CATEGORY",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SYS_CATEGORY");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SYS_CATEGORY");
        }
    }
}
