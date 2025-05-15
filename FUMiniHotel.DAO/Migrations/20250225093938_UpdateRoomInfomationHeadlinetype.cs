using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUMiniHotel.DAO.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomInfomationHeadlinetype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RoomHeadline",
                table: "RoomInformation",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RoomHeadline",
                table: "RoomInformation",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
