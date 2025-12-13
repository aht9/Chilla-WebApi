using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chilla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixNotificationRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "NotificationLogs");
            
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "NotificationLogs",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "NotificationLogs",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);
        }
    }
}
