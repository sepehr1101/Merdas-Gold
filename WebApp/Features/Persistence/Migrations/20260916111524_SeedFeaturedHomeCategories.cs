using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFeaturedHomeCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductCategory",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnHome",
                table: "ProductCategory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ImageUrl", "ShowOnHome" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DisplayOrder", "ImageUrl", "Name", "ShowOnHome" },
                values: new object[] { 5, "/assets/storefront/images/Untitled-4-min.png", "حلقه", true });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DisplayOrder", "ImageUrl", "ShowOnHome" },
                values: new object[] { 3, "/assets/storefront/images/Untitled-44-min.png", true });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DisplayOrder", "ImageUrl", "ShowOnHome" },
                values: new object[] { 2, "/assets/storefront/images/Untitled72-4-min.png", true });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "DisplayOrder", "ImageUrl", "ShowOnHome" },
                values: new object[] { 6, "/assets/storefront/images/421-min.png", true });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "DisplayOrder", "ImageUrl", "Name", "ShowOnHome" },
                values: new object[] { 4, "/assets/storefront/images/Untitled-q4-min.png", "آویز", true });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "DisplayOrder", "ImageUrl", "ShowOnHome" },
                values: new object[] { 7, null, false });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ImageUrl", "ShowOnHome" },
                values: new object[] { null, false });

            migrationBuilder.InsertData(
                table: "ProductCategory",
                columns: new[] { "Id", "Description", "DisplayOrder", "ImageUrl", "IsActive", "Name", "ParentId", "ShowOnHome", "Slug" },
                values: new object[] { 1009, "", 1, "/assets/storefront/images/Untitl555ed-4-min.png", true, "پابند", 1, true, "anklets" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 1009);

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "ShowOnHome",
                table: "ProductCategory");

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DisplayOrder", "Name" },
                values: new object[] { 1, "انگشتر" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 3,
                column: "DisplayOrder",
                value: 2);

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 4,
                column: "DisplayOrder",
                value: 3);

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 5,
                column: "DisplayOrder",
                value: 4);

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "DisplayOrder", "Name" },
                values: new object[] { 5, "پلاک و آویز" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 7,
                column: "DisplayOrder",
                value: 6);
        }
    }
}
