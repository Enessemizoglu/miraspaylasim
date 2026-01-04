using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MirasPaylasim.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExtendedZumreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FatherAlive",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MaternalGrandfatherAlive",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaternalGrandfatherUnclesAuntsAliveCount",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MaternalGrandmotherAlive",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaternalGrandmotherUnclesAuntsAliveCount",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaternalPredeceasedSiblingsChildrenCounts",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaternalSiblingsAliveCount",
                table: "Hesaplamalar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MotherAlive",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PaternalGrandfatherAlive",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaternalGrandfatherUnclesAuntsAliveCount",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PaternalGrandmotherAlive",
                table: "Hesaplamalar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaternalGrandmotherUnclesAuntsAliveCount",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaternalPredeceasedSiblingsChildrenCounts",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PaternalSiblingsAliveCount",
                table: "Hesaplamalar",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FatherAlive",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalGrandfatherAlive",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalGrandfatherUnclesAuntsAliveCount",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalGrandmotherAlive",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalGrandmotherUnclesAuntsAliveCount",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalPredeceasedSiblingsChildrenCounts",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MaternalSiblingsAliveCount",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "MotherAlive",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalGrandfatherAlive",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalGrandfatherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalGrandfatherUnclesAuntsAliveCount",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalGrandmotherAlive",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalGrandmotherPredeceasedUnclesAuntsChildrenCounts",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalGrandmotherUnclesAuntsAliveCount",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalPredeceasedSiblingsChildrenCounts",
                table: "Hesaplamalar");

            migrationBuilder.DropColumn(
                name: "PaternalSiblingsAliveCount",
                table: "Hesaplamalar");
        }
    }
}
