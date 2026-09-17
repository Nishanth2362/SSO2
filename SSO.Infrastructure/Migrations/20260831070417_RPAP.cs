using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RPAP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagementTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LaunchTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CallbackUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    State = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsConsumed = table.Column<bool>(type: "bit", nullable: false),
                    ConsumedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ConsumedUserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResultCodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ResultCodeExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsResultCodeConsumed = table.Column<bool>(type: "bit", nullable: false),
                    ResultCodeConsumedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RevokedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementTransactions_ClientId_Status",
                table: "ManagementTransactions",
                columns: new[] { "ClientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementTransactions_ExpiresOn",
                table: "ManagementTransactions",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementTransactions_LaunchTokenHash",
                table: "ManagementTransactions",
                column: "LaunchTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementTransactions_ResultCodeHash",
                table: "ManagementTransactions",
                column: "ResultCodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementTransactions_TenantId_Status",
                table: "ManagementTransactions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagementTransactions");
        }
    }
}
