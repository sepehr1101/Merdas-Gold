using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MerdasGold.Features.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecuritySettingsAndOpLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OperationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecuritySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    MinimumPasswordLength = table.Column<int>(type: "int", nullable: false),
                    RequireUppercase = table.Column<bool>(type: "bit", nullable: false),
                    RequireLowercase = table.Column<bool>(type: "bit", nullable: false),
                    RequireDigit = table.Column<bool>(type: "bit", nullable: false),
                    RequireSpecialCharacter = table.Column<bool>(type: "bit", nullable: false),
                    MaxFailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuritySettings", x => x.Id);
                    table.CheckConstraint("CK_SecuritySettings_LockoutDurationMinutes", "[LockoutDurationMinutes] BETWEEN 1 AND 1440");
                    table.CheckConstraint("CK_SecuritySettings_MaxFailedLoginAttempts", "[MaxFailedLoginAttempts] BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_SecuritySettings_MinimumPasswordLength", "[MinimumPasswordLength] BETWEEN 6 AND 128");
                    table.CheckConstraint("CK_SecuritySettings_SessionTimeoutMinutes", "[SessionTimeoutMinutes] BETWEEN 5 AND 10080");
                    table.CheckConstraint("CK_SecuritySettings_SingleRow", "[Id] = 1");
                });

            migrationBuilder.InsertData(
                table: "SecuritySettings",
                columns: new[] { "Id", "LockoutDurationMinutes", "MaxFailedLoginAttempts", "MinimumPasswordLength", "RequireDigit", "RequireLowercase", "RequireSpecialCharacter", "RequireUppercase", "SessionTimeoutMinutes" },
                values: new object[] { 1, 15, 5, 12, true, true, true, true, 60 });

            migrationBuilder.CreateIndex(
                name: "IX_OpLog_OperationDateTime",
                table: "OpLog",
                column: "OperationDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_OpLog_UserId",
                table: "OpLog",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpLog");

            migrationBuilder.DropTable(
                name: "SecuritySettings");
        }
    }
}
