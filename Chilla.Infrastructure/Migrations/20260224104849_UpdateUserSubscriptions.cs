using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chilla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyProgresses_UserSubscriptions_UserSubscriptionId",
                table: "DailyProgresses");

            migrationBuilder.DropIndex(
                name: "IX_DailyProgresses_ScheduledDate",
                table: "DailyProgresses");

            migrationBuilder.DropIndex(
                name: "IX_DailyProgresses_UserSubscriptionId",
                table: "DailyProgresses");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "DailyProgresses");

            migrationBuilder.DropColumn(
                name: "LateReason",
                table: "DailyProgresses");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "DailyProgresses");

            migrationBuilder.DropColumn(
                name: "UserSubscriptionId",
                table: "DailyProgresses");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "DailyProgresses",
                newName: "DayNumber");

            migrationBuilder.RenameColumn(
                name: "PlanTemplateItemId",
                table: "DailyProgresses",
                newName: "TaskId");

            migrationBuilder.AddColumn<bool>(
                name: "HasSignedCovenant",
                table: "UserSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CountCompleted",
                table: "DailyProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDone",
                table: "DailyProgresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "DailyProgresses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_DailyProgresses_Subscription_Task_Day",
                table: "DailyProgresses",
                columns: new[] { "SubscriptionId", "TaskId", "DayNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyProgresses_UserSubscriptions_SubscriptionId",
                table: "DailyProgresses",
                column: "SubscriptionId",
                principalTable: "UserSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyProgresses_UserSubscriptions_SubscriptionId",
                table: "DailyProgresses");

            migrationBuilder.DropIndex(
                name: "IX_DailyProgresses_Subscription_Task_Day",
                table: "DailyProgresses");

            migrationBuilder.DropColumn(
                name: "HasSignedCovenant",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CountCompleted",
                table: "DailyProgresses");

            migrationBuilder.DropColumn(
                name: "IsDone",
                table: "DailyProgresses");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "DailyProgresses");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "DailyProgresses",
                newName: "PlanTemplateItemId");

            migrationBuilder.RenameColumn(
                name: "DayNumber",
                table: "DailyProgresses",
                newName: "Value");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "DailyProgresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LateReason",
                table: "DailyProgresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "DailyProgresses",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UserSubscriptionId",
                table: "DailyProgresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyProgresses_ScheduledDate",
                table: "DailyProgresses",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyProgresses_UserSubscriptionId",
                table: "DailyProgresses",
                column: "UserSubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyProgresses_UserSubscriptions_UserSubscriptionId",
                table: "DailyProgresses",
                column: "UserSubscriptionId",
                principalTable: "UserSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
