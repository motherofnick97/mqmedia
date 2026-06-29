using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MqSocial.Migrations
{
    /// <inheritdoc />
    public partial class add_review : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxReviewTime",
                table: "Contracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReviewResult",
                table: "ContractKols",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractKolReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Review = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractKolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractKolReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractKolReviews_ContractKols_ContractKolId",
                        column: x => x.ContractKolId,
                        principalTable: "ContractKols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractKolReviews_ContractKolId",
                table: "ContractKolReviews",
                column: "ContractKolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractKolReviews");

            migrationBuilder.DropColumn(
                name: "MaxReviewTime",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ReviewResult",
                table: "ContractKols");
        }
    }
}
