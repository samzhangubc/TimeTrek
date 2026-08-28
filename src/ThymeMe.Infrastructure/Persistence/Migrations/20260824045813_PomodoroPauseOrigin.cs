using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThymeMe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PomodoroPauseOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PausedFromStatus",
                table: "TimingRoots",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PausedFromStatus",
                table: "TimingRoots");
        }
    }
}
