using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MirasPaylasim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShareReservedPortionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TheoreticalAmount",
                table: "Paylar",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedPortion",
                table: "Paylar",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsReservedPortionViolated",
                table: "Paylar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ViolationAmount",
                table: "Paylar",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TheoreticalAmount",
                table: "Paylar");

            migrationBuilder.DropColumn(
                name: "ReservedPortion",
                table: "Paylar");

            migrationBuilder.DropColumn(
                name: "IsReservedPortionViolated",
                table: "Paylar");

            migrationBuilder.DropColumn(
                name: "ViolationAmount",
                table: "Paylar");
        }
    }
}




