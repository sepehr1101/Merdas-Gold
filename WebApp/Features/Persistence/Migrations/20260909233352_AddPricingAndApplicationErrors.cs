using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndApplicationErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", maxLength: 24000, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationErrors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoldRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PriceToman = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
                    StartsUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceDiscounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    FeeMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FeeValue = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
                    ProfitPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    RoundToToman = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
                    InvoiceFooter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProtectedApiKey = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SourceUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxAgeMinutes = table.Column<int>(type: "int", nullable: false),
                    ManualPrice = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: true),
                    ManualExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PricingRules",
                columns: new[] { "Id", "FeeMode", "FeeValue", "InvoiceFooter", "ProfitPercent", "RoundToToman", "TaxPercent" },
                values: new object[] { 1, "percent", 0m, "از اعتماد شما سپاسگزاریم.", 0m, 1m, 0m });

            migrationBuilder.InsertData(
                table: "RateSettings",
                columns: new[] { "Id", "Enabled", "IntervalMinutes", "ManualExpiresUtc", "ManualPrice", "MaxAgeMinutes", "ProtectedApiKey", "Provider", "SourceUnit" },
                values: new object[] { 1, false, 5, null, null, 10, "", "Navasan", "toman" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationErrors_OccurredUtc",
                table: "ApplicationErrors",
                column: "OccurredUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationErrors_Source_StatusCode_OccurredUtc",
                table: "ApplicationErrors",
                columns: new[] { "Source", "StatusCode", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationErrors_TraceId",
                table: "ApplicationErrors",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldRates_Provider_IsValid_Id",
                table: "GoldRates",
                columns: new[] { "Provider", "IsValid", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GoldRates_ReceivedUtc",
                table: "GoldRates",
                column: "ReceivedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PriceDiscounts_Enabled_StartsUtc_EndsUtc",
                table: "PriceDiscounts",
                columns: new[] { "Enabled", "StartsUtc", "EndsUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationErrors");

            migrationBuilder.DropTable(
                name: "GoldRates");

            migrationBuilder.DropTable(
                name: "PriceDiscounts");

            migrationBuilder.DropTable(
                name: "PricingRules");

            migrationBuilder.DropTable(
                name: "RateSettings");
        }
    }
}
