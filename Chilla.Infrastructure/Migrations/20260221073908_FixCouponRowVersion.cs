using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chilla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCouponRowVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ۱. حذف ستون قبلی که varbinary بود
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Coupons");

            // ۲. ساخت مجدد ستون با نوع rowversion
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Coupons",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Coupons");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Coupons",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
