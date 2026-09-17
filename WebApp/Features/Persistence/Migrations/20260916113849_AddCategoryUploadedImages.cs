using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryUploadedImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "ProductCategory",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "ProductCategory",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 1009,
                columns: new[] { "ImageContentType", "ImageData" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "ProductCategory");
        }
    }
}
