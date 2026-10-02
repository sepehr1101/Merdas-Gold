using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations;

public partial class PersistMarketRateSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "MarketSnapshotJson", table: "GoldRates", type: "nvarchar(max)", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "MarketSnapshotJson", table: "GoldRates");
}