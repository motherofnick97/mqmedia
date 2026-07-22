using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MqSocial.Migrations
{
    /// <inheritdoc />
    public partial class Added_Contract_ProductName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "Contracts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "Contracts");
        }
    }
}
