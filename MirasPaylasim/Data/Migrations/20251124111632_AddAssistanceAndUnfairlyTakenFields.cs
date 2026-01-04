using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MirasPaylasim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistanceAndUnfairlyTakenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssistanceList",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasAssistance",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasUnfairlyTaken",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UnfairlyTakenList",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssistanceList",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "HasAssistance",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "HasUnfairlyTaken",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "UnfairlyTakenList",
                table: "Hesaplamalar");
        }
    }
}
