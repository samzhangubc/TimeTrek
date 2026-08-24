using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTrek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DurableSessionDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_IsDeleted_StartUtcMilliseconds",
                table: "Sessions");

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IsDraft_IsDeleted_StartUtcMilliseconds",
                table: "Sessions",
                columns: new[] { "IsDraft", "IsDeleted", "StartUtcMilliseconds" });

            migrationBuilder.AddForeignKey(
                name: "FK_TimingSegments_Sessions_SessionId",
                table: "TimingSegments",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimingSegments_Sessions_SessionId",
                table: "TimingSegments");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_IsDraft_IsDeleted_StartUtcMilliseconds",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IsDeleted_StartUtcMilliseconds",
                table: "Sessions",
                columns: new[] { "IsDeleted", "StartUtcMilliseconds" });
        }
    }
}
