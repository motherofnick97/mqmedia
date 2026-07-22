using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MqSocial.Migrations
{
    /// <inheritdoc />
    public partial class update_index_kol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kols_AccountId_Channel",
                table: "Kols");

            migrationBuilder.CreateIndex(
                name: "IX_Kols_AccountId_Channel",
                table: "Kols",
                columns: new[] { "AccountId", "Channel" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kols_AccountId_Channel",
                table: "Kols");

            migrationBuilder.CreateIndex(
                name: "IX_Kols_AccountId_Channel",
                table: "Kols",
                columns: new[] { "AccountId", "Channel" },
                unique: true);
        }
    }
}
