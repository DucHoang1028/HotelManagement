using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUMiniHotel.DAO.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AskedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AnsweredById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AskedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAnswered = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_AspNetUsers_AnsweredById",
                        column: x => x.AnsweredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Questions_AspNetUsers_AskedById",
                        column: x => x.AskedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AnsweredById",
                table: "Questions",
                column: "AnsweredById");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AskedById",
                table: "Questions",
                column: "AskedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Questions");
        }
    }
}
