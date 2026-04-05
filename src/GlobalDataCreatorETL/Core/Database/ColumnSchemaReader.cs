using GlobalDataCreatorETL.Core.Database;
using GlobalDataCreatorETL.Core.Models;
using Microsoft.Data.SqlClient;

namespace GlobalDataCreatorETL.Core.Database;

/// <summary>
/// Reads column schema from INFORMATION_SCHEMA.COLUMNS for a given temp table.
/// Used by the Excel engine to determine date/numeric/text formatting per column.
/// </summary>
public sealed class ColumnSchemaReader
{
    private readonly DatabaseConnectionService _connectionService;

    public ColumnSchemaReader(DatabaseConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IReadOnlyList<ColumnInfo>> GetColumnInfoAsync(string tableName, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COLUMN_NAME, DATA_TYPE, ORDINAL_POSITION,
                   CASE IS_NULLABLE WHEN 'YES' THEN 1 ELSE 0 END AS IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName
            ORDER BY ORDINAL_POSITION
            """;

        var result = new List<ColumnInfo>();

        await using var conn = new SqlConnection(_connectionService.BuildConnectionString());
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ColumnInfo(
                ColumnName: reader.GetString(0),
                DataType: reader.GetString(1),
                OrdinalPosition: reader.GetInt32(2),
                IsNullable: reader.GetInt32(3) == 1
            ));
        }

        return result;
    }
}
