using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreBankAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Iban = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreBankAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreLocation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    ZoomLevel = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLocation", x => x.Id);
                    table.CheckConstraint("CK_StoreLocation_Latitude", "[Latitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_StoreLocation_Longitude", "[Longitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_StoreLocation_SingleRow", "[Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "StoreProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tagline = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BusinessCategory = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivityStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LogoData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    FaviconData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FaviconContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FaviconFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProfile", x => x.Id);
                    table.CheckConstraint("CK_StoreProfile_SingleRow", "[Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "StoreWorkingHour",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DayOrder = table.Column<int>(type: "int", nullable: false),
                    DayName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreWorkingHour", x => x.Id);
                    table.CheckConstraint("CK_StoreWorkingHour_DayOrder", "[DayOrder] BETWEEN 0 AND 6");
                });

            migrationBuilder.InsertData(
                table: "StoreLocation",
                columns: new[] { "Id", "Address", "Latitude", "Longitude", "ZoomLevel" },
                values: new object[] { 1, "", 35.689200m, 51.389000m, 13 });

            migrationBuilder.InsertData(
                table: "StoreProfile",
                columns: new[] { "Id", "ActivityStartDate", "BusinessCategory", "EnglishName", "FaviconContentType", "FaviconData", "FaviconFileName", "IsActive", "LogoContentType", "LogoData", "LogoFileName", "Name", "ShortDescription", "Tagline" },
                values: new object[] { 1, null, "طلا و جواهر", "Merdas Gold", null, null, null, true, null, null, null, "مرداس گلد", "", "" });

            migrationBuilder.InsertData(
                table: "StoreWorkingHour",
                columns: new[] { "Id", "DayName", "DayOrder", "EndTime", "IsOpen", "StartTime" },
                values: new object[,]
                {
                    { 1, "شنبه", 0, new TimeOnly(21, 0, 0), true, new TimeOnly(9, 0, 0) },
                    { 2, "یکشنبه", 1, new TimeOnly(21, 0, 0), true, new TimeOnly(9, 0, 0) },
                    { 3, "دوشنبه", 2, new TimeOnly(21, 0, 0), true, new TimeOnly(9, 0, 0) },
                    { 4, "سه‌شنبه", 3, new TimeOnly(21, 0, 0), true, new TimeOnly(9, 0, 0) },
                    { 5, "چهارشنبه", 4, new TimeOnly(21, 0, 0), true, new TimeOnly(9, 0, 0) },
                    { 6, "پنجشنبه", 5, new TimeOnly(18, 0, 0), true, new TimeOnly(9, 0, 0) },
                    { 7, "جمعه", 6, null, false, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreBankAccount_IsDefault",
                table: "StoreBankAccount",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StoreWorkingHour_DayOrder",
                table: "StoreWorkingHour",
                column: "DayOrder",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreBankAccount");

            migrationBuilder.DropTable(
                name: "StoreLocation");

            migrationBuilder.DropTable(
                name: "StoreProfile");

            migrationBuilder.DropTable(
                name: "StoreWorkingHour");
        }
    }
}
