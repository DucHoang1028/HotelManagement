using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUMiniHotel.DAO.Migrations
{
    /// <inheritdoc />
    public partial class addingfeature1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeature",
                table: "RoomType");

            migrationBuilder.AddColumn<bool>(
                name: "IsFeature",
                table: "RoomInformation",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeature",
                table: "RoomInformation");

            migrationBuilder.AddColumn<bool>(
                name: "IsFeature",
                table: "RoomType",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
