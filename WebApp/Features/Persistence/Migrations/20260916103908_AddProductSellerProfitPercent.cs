using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSellerProfitPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SellerProfitPercent",
                table: "Product",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_SellerProfitPercent",
                table: "Product",
                sql: "[SellerProfitPercent] IS NULL OR [SellerProfitPercent] BETWEEN 0 AND 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_SellerProfitPercent",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "SellerProfitPercent",
                table: "Product");
        }
    }
}
