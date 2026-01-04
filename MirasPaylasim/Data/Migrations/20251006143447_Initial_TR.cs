using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MirasPaylasim.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial_TR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hesaplamalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAssets = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Receivables = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Debts = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SpouseStatus = table.Column<int>(type: "int", nullable: false),
                    SpouseDeathTiming = table.Column<int>(type: "int", nullable: false),
                    LivingChildrenCount = table.Column<int>(type: "int", nullable: false),
                    HasPredeceasedChild = table.Column<bool>(type: "bit", nullable: false),
                    HasGrandchildrenFromPredeceasedChild = table.Column<bool>(type: "bit", nullable: false),
                    PredeceasedChildNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentsAlive = table.Column<int>(type: "int", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hesaplamalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paylar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    HeirType = table.Column<int>(type: "int", nullable: false),
                    HeirDisplay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FractionNumerator = table.Column<int>(type: "int", nullable: false),
                    FractionDenominator = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paylar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paylar_Hesaplamalar_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Hesaplamalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paylar_CalculationId",
                table: "Paylar",
                column: "CalculationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Paylar");

            migrationBuilder.DropTable(
                name: "Hesaplamalar");
        }
    }
}
