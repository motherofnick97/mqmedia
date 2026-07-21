using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MqSocial.Migrations
{
    /// <inheritdoc />
    public partial class Added_KolGeneral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KolGeneralId",
                table: "Kols",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KolGenerals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Dob = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Identity = table.Column<string>(type: "text", nullable: true),
                    Bank = table.Column<int>(type: "integer", nullable: false),
                    BankNumber = table.Column<string>(type: "text", nullable: true),
                    BankOwner = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KolGenerals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kols_KolGeneralId",
                table: "Kols",
                column: "KolGeneralId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kols_KolGenerals_KolGeneralId",
                table: "Kols",
                column: "KolGeneralId",
                principalTable: "KolGenerals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kols_KolGenerals_KolGeneralId",
                table: "Kols");

            migrationBuilder.DropTable(
                name: "KolGenerals");

            migrationBuilder.DropIndex(
                name: "IX_Kols_KolGeneralId",
                table: "Kols");

            migrationBuilder.DropColumn(
                name: "KolGeneralId",
                table: "Kols");
        }
    }
}
