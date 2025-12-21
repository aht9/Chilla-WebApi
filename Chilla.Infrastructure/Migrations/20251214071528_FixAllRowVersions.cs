using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chilla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAllRowVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. RolePermissions
            migrationBuilder.DropColumn(name: "RowVersion", table: "RolePermissions");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "RolePermissions", type: "rowversion", rowVersion: true, nullable: false);

            // 2. PlanTemplateItems
            migrationBuilder.DropColumn(name: "RowVersion", table: "PlanTemplateItems");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "PlanTemplateItems", type: "rowversion", rowVersion: true, nullable: false);

            // 3. DailyProgresses
            migrationBuilder.DropColumn(name: "RowVersion", table: "DailyProgresses");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "DailyProgresses", type: "rowversion", rowVersion: true, nullable: false);

            // 4. BlockedIps
            migrationBuilder.DropColumn(name: "RowVersion", table: "BlockedIps");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "BlockedIps", type: "rowversion", rowVersion: true, nullable: false);

            // 5. RequestLogs
            migrationBuilder.DropColumn(name: "RowVersion", table: "RequestLogs");
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "RequestLogs", type: "rowversion", rowVersion: true, nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "RolePermissions",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "RequestLogs",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "PlanTemplateItems",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "DailyProgresses",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "BlockedIps",
                type: "varbinary(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);
        }
    }
}
