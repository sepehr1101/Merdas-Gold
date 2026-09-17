using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMakingFeeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MakingFeePercent",
                table: "Product",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: 1,
                column: "RoundToToman",
                value: 10000m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_MakingFeePercent",
                table: "Product",
                sql: "[MakingFeePercent] IS NULL OR [MakingFeePercent] BETWEEN 0 AND 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_MakingFeePercent",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "MakingFeePercent",
                table: "Product");

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: 1,
                column: "RoundToToman",
                value: 1m);
        }
    }
}
