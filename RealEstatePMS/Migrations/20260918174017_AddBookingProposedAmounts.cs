using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstatePMS.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingProposedAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProposedAmount",
                table: "BookingRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProposedDeposit",
                table: "BookingRequests",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedAmount",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "ProposedDeposit",
                table: "BookingRequests");
        }
    }
}
