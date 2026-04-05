using GlobalDataCreatorETL.Core.Configuration;
using GlobalDataCreatorETL.Core.Database;
using Microsoft.Data.SqlClient;

namespace GlobalDataCreatorETL.Core.DataAccess;

/// <summary>
/// Executes the Import stored procedure and streams data from the Import view.
/// Note: uses @ImpCmp instead of @ExpCmp, otherwise identical pattern to ExportDataAccess.
/// </summary>
public sealed class ImportDataAccess
{
    private readonly DatabaseConnectionService _connectionService;
    private readonly ConfigurationCacheService _config;

    public ImportDataAccess(DatabaseConnectionService connectionService, ConfigurationCacheService config)
    {
        _connectionService = connectionService;
        _config = config;
    }

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
