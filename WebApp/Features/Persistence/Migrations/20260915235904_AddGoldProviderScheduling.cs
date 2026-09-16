using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldProviderScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetentionDays",
                table: "RateSettings",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.UpdateData(
                table: "RateSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Enabled", "IntervalMinutes", "MaxAgeMinutes", "Provider", "RetentionDays" },
                values: new object[] { true, 1, 5, "TabanGohar", 90 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetentionDays",
                table: "RateSettings");

            migrationBuilder.UpdateData(
                table: "RateSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Enabled", "IntervalMinutes", "MaxAgeMinutes", "Provider" },
                values: new object[] { false, 5, 10, "Navasan" });
        }
    }
}
