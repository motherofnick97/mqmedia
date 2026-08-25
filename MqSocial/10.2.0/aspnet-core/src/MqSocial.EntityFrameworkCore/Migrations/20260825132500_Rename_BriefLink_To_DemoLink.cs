using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MqSocial.Migrations
{
    /// <inheritdoc />
    public partial class Rename_BriefLink_To_DemoLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BriefLink",
                table: "ContractKols",
                newName: "DemoLink");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DemoLink",
                table: "ContractKols",
                newName: "BriefLink");
        }
    }
}
