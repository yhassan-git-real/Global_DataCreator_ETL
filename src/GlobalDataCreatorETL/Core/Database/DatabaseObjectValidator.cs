using GlobalDataCreatorETL.Core.Database;
using Microsoft.Data.SqlClient;

namespace GlobalDataCreatorETL.Core.Database;

/// <summary>
/// Validates that Views and Stored Procedures actually exist in the database.
/// </summary>
public sealed class DatabaseObjectValidator
{
    private readonly DatabaseConnectionService _connectionService;

    public DatabaseObjectValidator(DatabaseConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<bool> ViewExistsAsync(string viewName)
    {
        const string sql = """
            SELECT COUNT(1) FROM INFORMATION_SCHEMA.VIEWS
            WHERE TABLE_NAME = @name
            """;
        return await ExistsAsync(sql, viewName);
    }

    public async Task<bool> SpExistsAsync(string spName)
    {
        const string sql = """
            SELECT COUNT(1) FROM INFORMATION_SCHEMA.ROUTINES
            WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = @name
            """;
        return await ExistsAsync(sql, spName);
    }

    private async Task<bool> ExistsAsync(string sql, string name)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionService.BuildConnectionString());
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", name);
            var count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }
        catch
        {
            return false;
        }
    }
}
