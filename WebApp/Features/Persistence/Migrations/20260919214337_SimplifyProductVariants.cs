using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPiece");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariant_Barcode",
                table: "ProductVariant");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariant_Sku",
                table: "ProductVariant");

            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "ProductVariant",
                newName: "SizeValue");

            migrationBuilder.RenameColumn(
                name: "Barcode",
                table: "ProductVariant",
                newName: "InternalCode");

            migrationBuilder.AddColumn<decimal>(
                name: "ExactGoldWeightGrams",
                table: "ProductVariant",
                type: "decimal(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ProductVariant",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ProductVariant",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSelectableForProducts",
                table: "ProductCategory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductTypeId",
                table: "ProductCategory",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SizeLabel",
                table: "ProductCategory",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SizeMode",
                table: "ProductCategory",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SizeOptions",
                table: "ProductCategory",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SizeUnit",
                table: "ProductCategory",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { false, null, "طول", "none", "", "" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 1, "سایز", "select", "بچه‌گانه، کوچک، بزرگ", "" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 2, "طول", "number", "", "سانتی‌متر" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 3, "طول", "number", "", "سانتی‌متر" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 4, "طول", "none", "", "" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 5, "طول", "none", "", "" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 6, "طول", "none", "", "" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 7, "طول", "none", "", "" });

            migrationBuilder.UpdateData(
                table: "ProductCategory",
                keyColumn: "Id",
                keyValue: 1009,
                columns: new[] { "IsSelectableForProducts", "ProductTypeId", "SizeLabel", "SizeMode", "SizeOptions", "SizeUnit" },
                values: new object[] { true, 3, "طول", "number", "", "سانتی‌متر" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_InternalCode",
                table: "ProductVariant",
                column: "InternalCode",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariant_Quantity",
                table: "ProductVariant",
                sql: "[Quantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariant_Status",
                table: "ProductVariant",
                sql: "[Status] IN (N'available',N'unavailable')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariant_Weight",
                table: "ProductVariant",
                sql: "[ExactGoldWeightGrams] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategory_ProductTypeId",
                table: "ProductCategory",
                column: "ProductTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_ProductType_ProductTypeId",
                table: "ProductCategory",
                column: "ProductTypeId",
                principalTable: "ProductType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_ProductType_ProductTypeId",
                table: "ProductCategory");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariant_InternalCode",
                table: "ProductVariant");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariant_Quantity",
                table: "ProductVariant");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariant_Status",
                table: "ProductVariant");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariant_Weight",
                table: "ProductVariant");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategory_ProductTypeId",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "ExactGoldWeightGrams",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "IsSelectableForProducts",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "SizeLabel",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "SizeMode",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "SizeOptions",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "SizeUnit",
                table: "ProductCategory");

            migrationBuilder.RenameColumn(
                name: "SizeValue",
                table: "ProductVariant",
                newName: "Sku");

            migrationBuilder.RenameColumn(
                name: "InternalCode",
                table: "ProductVariant",
                newName: "Barcode");

            migrationBuilder.CreateTable(
                name: "ProductPiece",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    ExactGoldWeightGrams = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StoneWeightCarats = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: true),
                    TrackingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPiece", x => x.Id);
                    table.CheckConstraint("CK_ProductPiece_Quantity", "[Quantity] >= 0");
                    table.CheckConstraint("CK_ProductPiece_Status", "[Status] IN (N'available',N'reserved',N'sold',N'damaged')");
                    table.CheckConstraint("CK_ProductPiece_Weight", "[ExactGoldWeightGrams] > 0");
                    table.ForeignKey(
                        name: "FK_ProductPiece_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_Barcode",
                table: "ProductVariant",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_Sku",
                table: "ProductVariant",
                column: "Sku",
                unique: true,
                filter: "[Sku] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPiece_ProductVariantId",
                table: "ProductPiece",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPiece_TrackingCode",
                table: "ProductPiece",
                column: "TrackingCode",
                unique: true);
        }
    }
}
