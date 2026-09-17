using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SSO.Common.Constants.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Infrastructure.Contexts
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {      
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Find appsettings.json (works from Infrastructure or Web project)
            var basePath = Directory.GetCurrentDirectory();
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
                basePath = Path.Combine(basePath, ".."); // adjust if needed: "../.." etc.

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var provider = configuration.GetValue<string>("DbSettings:Provider")?.ToLower();
            var conn = configuration.GetValue<string>("DbSettings:DefaultConnection")!
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // AUTO-DETECT ALL 4 PROVIDERS (including MySQL official provider)
            if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Oracle.ToLower()))
            {
                optionsBuilder.UseOracle(conn, o => o.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion23));
            }
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Mysql.ToLower()))
            {
                // MySQL / MariaDB (MySqlConnector or Pomelo)
                optionsBuilder.UseMySQL(conn);
            }
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.SqlServer.ToLower()))
            {
                optionsBuilder.UseSqlServer(conn);
            }
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.PostgreSql.ToLower()))
            {
                optionsBuilder.UseNpgsql(conn);
            }
            else // ← MySQL (both Server= and Host= styles work)
            {
                throw new NotSupportedException($"Database provider not recognized from connection string.");
            }

            optionsBuilder.EnableSensitiveDataLogging();
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
