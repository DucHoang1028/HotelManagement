using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUMiniHotel.DAO.Migrations
{
    /// <inheritdoc />
    public partial class updateroominfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumOfBath",
                table: "RoomInformation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumOfBed",
                table: "RoomInformation",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumOfBath",
                table: "RoomInformation");

            migrationBuilder.DropColumn(
                name: "NumOfBed",
                table: "RoomInformation");
        }
    }
}
