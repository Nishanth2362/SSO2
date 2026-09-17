using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Constants.Application;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;

namespace SSO.Infrastructure.Services.Repositories
{
    public class DapperRepository : IDapperRepository
    {
        private IDbConnection connection;
        private readonly IConfiguration _configuration;
        public DapperRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            connection =  GetConnection(configuration);
        }

        private IDbConnection GetConnection(IConfiguration configuration)
        {
            var provider = configuration.GetValue<string>("DbSettings:Provider")?.ToLower();
            var conn = configuration.GetValue<string>("DbSettings:DefaultConnection")!
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.SqlServer.ToLower()))
            {
                return new SqlConnection(conn);
            }
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Mysql.ToLower()))
            {

                // MySQL / MariaDB (MySqlConnector or Pomelo)
                return new MySqlConnection(conn);
            }
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.Oracle.ToLower()))
            {
                return new OracleConnection(conn);
            }
           
            else if (provider!.ToLower().Equals(ApplicationConstants.DBProvider.PostgreSql.ToLower()))
            {
                return new Npgsql.NpgsqlConnection(conn);
            }
            else // ← MySQL (both Server= and Host= styles work)
            {
                throw new NotSupportedException($"Database provider not recognized from connection string.");
            }
        }

        public async Task<List<T>> QuerySPAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : class
        {
            return (await connection.QueryAsync<T>(sql, param, transaction, null, CommandType.StoredProcedure)).AsList();
        }

        public async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await connection.ExecuteAsync(sql, param, transaction, null, CommandType.StoredProcedure);
        }

        public async Task<int> ExecuteNonSPAsync(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await connection.ExecuteAsync(sql, param, transaction, null);
        }

        public async Task<List<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : class
        {
            return (await connection.QueryAsync<T>(sql, param, transaction)).AsList();
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : class
        {
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : class
        {
            return await connection.QuerySingleAsync<T>(sql, param, transaction);
        }
        public async Task<IDataReader> ExecuteReaderAsync(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await connection.ExecuteReaderAsync(sql, param, transaction);
        }
        public void Dispose()
        {
            connection.Dispose();
        }

        public async Task BackUpDB()
        {
            connection = new SqlConnection(_configuration.GetConnectionString("BackUpConnection"));
        }

        public async Task<IDataReader> ExecuteReaderSpAsync(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await connection.ExecuteReaderAsync(sql, param, transaction, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> Exists(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await connection.ExecuteScalarAsync<bool>(sql, param, transaction, null);
        }
    }
}
