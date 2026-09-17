using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RPAPsettingsmanage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RpapSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultProfileTtlSeconds = table.Column<int>(type: "int", nullable: false),
                    DefaultManagementTtlSeconds = table.Column<int>(type: "int", nullable: false),
                    MinTtlSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxTtlSeconds = table.Column<int>(type: "int", nullable: false),
                    ProfileLandingUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsersLandingUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FallbackUnauthorizedRedirectUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoRevokeOnResultExchange = table.Column<bool>(type: "bit", nullable: false),
                    RequireStrictLocalhostPort = table.Column<bool>(type: "bit", nullable: false),
                    EnforceScopeAccess = table.Column<bool>(type: "bit", nullable: false),
                    ContextCookieExpiryDays = table.Column<int>(type: "int", nullable: false),
                    AllowedScopes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfileScopeAllowedActions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsersScopeAllowedActions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RpapSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RpapSettings");
        }
    }
}
