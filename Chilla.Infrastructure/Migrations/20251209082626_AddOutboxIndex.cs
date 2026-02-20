using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chilla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedDate",
                table: "OutboxMessages",
                column: "ProcessedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedDate",
                table: "OutboxMessages");
        }
    }
}
