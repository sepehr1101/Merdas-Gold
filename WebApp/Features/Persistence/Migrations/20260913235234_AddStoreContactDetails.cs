using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreContactDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "StoreProfile",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "StoreProfile",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "StoreLocation",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "Latitude", "Longitude", "ZoomLevel" },
                values: new object[] { "اصفهان، میدان امام علی، بازار طلا و جواهر خورشید، طبقه همکف، واحد ۱۲۰", 32.667300m, 51.688100m, 17 });

            migrationBuilder.UpdateData(
                table: "StoreProfile",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "PhoneNumber" },
                values: new object[] { "info@merdasgold.ir", "۰۹۱۳۳۳۸۸۸۱۹" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "StoreProfile");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "StoreProfile");

            migrationBuilder.UpdateData(
                table: "StoreLocation",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "Latitude", "Longitude", "ZoomLevel" },
                values: new object[] { "", 35.689200m, 51.389000m, 13 });
        }
    }
}
