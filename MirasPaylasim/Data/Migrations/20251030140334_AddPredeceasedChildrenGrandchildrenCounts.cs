using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MirasPaylasim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPredeceasedChildrenGrandchildrenCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PredeceasedChildrenGrandchildrenCounts",
                table: "Hesaplamalar",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PredeceasedChildrenGrandchildrenCounts",
                table: "Hesaplamalar");
        }
    }
}
