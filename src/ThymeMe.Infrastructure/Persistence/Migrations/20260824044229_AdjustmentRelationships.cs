using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThymeMe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdjustmentRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AdjustmentCategories_CategoryId",
                table: "AdjustmentCategories",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdjustmentCategories_Adjustments_AdjustmentId",
                table: "AdjustmentCategories",
                column: "AdjustmentId",
                principalTable: "Adjustments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdjustmentCategories_Categories_CategoryId",
                table: "AdjustmentCategories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdjustmentCategories_Adjustments_AdjustmentId",
                table: "AdjustmentCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_AdjustmentCategories_Categories_CategoryId",
                table: "AdjustmentCategories");

            migrationBuilder.DropIndex(
                name: "IX_AdjustmentCategories_CategoryId",
                table: "AdjustmentCategories");
        }
    }
}
