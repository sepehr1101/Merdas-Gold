using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreSocialNetworks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreSocialNetwork",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSocialNetwork", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "StoreSocialNetwork",
                columns: new[] { "Id", "BaseUrl", "DisplayName", "DisplayOrder", "IsActive", "Key", "Username" },
                values: new object[,]
                {
                    { 1, "https://www.instagram.com/", "اینستاگرام", 1, true, "instagram", "merdasgold" },
                    { 2, "https://t.me/", "تلگرام", 2, true, "telegram", "merdasgold2" },
                    { 3, "https://ble.ir/", "بله", 3, true, "bale", "merdasgold" },
                    { 4, "https://eitaa.com/", "ایتا", 4, true, "eitaa", "merdasgoldd" },
                    { 5, "https://rubika.ir/", "روبیکا", 5, true, "rubika", "merdasgold" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreSocialNetwork_DisplayOrder",
                table: "StoreSocialNetwork",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreSocialNetwork_Key",
                table: "StoreSocialNetwork",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreSocialNetwork");
        }
    }
}
