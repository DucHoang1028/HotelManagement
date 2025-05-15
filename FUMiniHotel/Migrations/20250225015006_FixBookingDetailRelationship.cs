using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUMiniHotel.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingDetailRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetail_RoomInformation_RoomID",
                table: "BookingDetail");

            migrationBuilder.AddColumn<int>(
                name: "BookingReservationID",
                table: "BookingDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetail_BookingReservationID",
                table: "BookingDetail",
                column: "BookingReservationID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetail_BookingReservations_BookingReservationID",
                table: "BookingDetail",
                column: "BookingReservationID",
                principalTable: "BookingReservations",
                principalColumn: "BookingReservationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetail_RoomInformation_RoomID",
                table: "BookingDetail",
                column: "RoomID",
                principalTable: "RoomInformation",
                principalColumn: "RoomID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetail_BookingReservations_BookingReservationID",
                table: "BookingDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingDetail_RoomInformation_RoomID",
                table: "BookingDetail");

            migrationBuilder.DropIndex(
                name: "IX_BookingDetail_BookingReservationID",
                table: "BookingDetail");

            migrationBuilder.DropColumn(
                name: "BookingReservationID",
                table: "BookingDetail");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingDetail_RoomInformation_RoomID",
                table: "BookingDetail",
                column: "RoomID",
                principalTable: "RoomInformation",
                principalColumn: "RoomID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
