using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MqSocial.Migrations
{
    /// <inheritdoc />
    public partial class update_kol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Kols");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Kols",
                type: "datetime2",
                nullable: true);
        }
    }
}
