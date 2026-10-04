using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstatePMS.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingNumber",
                table: "BookingRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_BookingNumber",
                table: "BookingRequests",
                column: "BookingNumber",
                unique: true,
                filter: "[BookingNumber] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_BookingNumber",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "BookingNumber",
                table: "BookingRequests");
        }
    }
}
