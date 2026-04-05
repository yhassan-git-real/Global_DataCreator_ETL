using GlobalDataCreatorETL.Core.Configuration;
using GlobalDataCreatorETL.Core.Database;
using Microsoft.Data.SqlClient;

namespace GlobalDataCreatorETL.Core.DataAccess;

/// <summary>
/// Executes the Export stored procedure and streams data from the Export view.
/// </summary>
public sealed class ExportDataAccess
{
    private readonly DatabaseConnectionService _connectionService;
    private readonly ConfigurationCacheService _config;

    public ExportDataAccess(DatabaseConnectionService connectionService, ConfigurationCacheService config)
    {
        _connectionService = connectionService;
        _config = config;
    }

    /// <summary>
    /// Executes the SP that populates the country's export temp table.
    /// </summary>
    public async Task ExecuteSpAsync(
        string spName,
        Dictionary<string, object> parameters,
        CancellationToken ct)
    {
        var timeout = _config.GetDbSettings().CommandTimeoutSeconds;
        await using var conn = new SqlConnection(_connectionService.BuildConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand($"dbo.{spName}", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = timeout
        };

        foreach (var (key, value) in parameters)
            cmd.Parameters.AddWithValue(key, value);

        _connectionService.Pause();
        try
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _connectionService.Resume();
        }
    }

    /// <summary>
    /// Returns a count of rows visible through the view (pre-flight check).
    /// </summary>
    public async Task<long> GetRowCountAsync(string viewName, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionService.BuildConnectionString());
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT COUNT_BIG(*) FROM [{viewName}]", conn)
        {
            CommandTimeout = _config.GetDbSettings().CommandTimeoutSeconds
        };
        return (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);
    }

    /// <summary>
    /// Returns a streaming SqlDataReader over the export view. Caller owns the connection.
    /// </summary>
    public async Task<(SqlConnection Connection, SqlDataReader Reader)> GetDataReaderAsync(
        string viewName,
        CancellationToken ct)
    {
        var conn = new SqlConnection(_connectionService.BuildConnectionString());
        await conn.OpenAsync(ct);
        var cmd = new SqlCommand($"SELECT * FROM [{viewName}]", conn)
        {
            CommandTimeout = _config.GetDbSettings().CommandTimeoutSeconds
        };
        var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection, ct);
        return (conn, reader);
    }
}
