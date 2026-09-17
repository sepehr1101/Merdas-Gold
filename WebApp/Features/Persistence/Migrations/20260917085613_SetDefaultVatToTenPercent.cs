using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultVatToTenPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Upgrade only stores still using the old seed value.
            migrationBuilder.Sql("UPDATE [PricingRules] SET [TaxPercent] = 10 WHERE [Id] = 1 AND [TaxPercent] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [PricingRules] SET [TaxPercent] = 0 WHERE [Id] = 1 AND [TaxPercent] = 10");
        }
    }
}
