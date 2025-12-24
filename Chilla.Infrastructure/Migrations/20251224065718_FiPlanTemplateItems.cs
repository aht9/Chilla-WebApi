using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chilla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FiPlanTemplateItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DayNumber",
                table: "PlanTemplateItems",
                newName: "StartDay");

            migrationBuilder.AddColumn<int>(
                name: "EndDay",
                table: "PlanTemplateItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequiredNotifications",
                table: "PlanTemplateItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDay",
                table: "PlanTemplateItems");

            migrationBuilder.DropColumn(
                name: "RequiredNotifications",
                table: "PlanTemplateItems");

            migrationBuilder.RenameColumn(
                name: "StartDay",
                table: "PlanTemplateItems",
                newName: "DayNumber");
        }
    }
}
