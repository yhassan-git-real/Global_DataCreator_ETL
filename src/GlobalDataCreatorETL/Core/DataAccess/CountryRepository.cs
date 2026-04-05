using GlobalDataCreatorETL.Core.Database;
using GlobalDataCreatorETL.Core.Models;
using Microsoft.Data.SqlClient;

namespace GlobalDataCreatorETL.Core.DataAccess;

/// <summary>
/// Queries dbo.mst_country for active countries and country metadata.
/// </summary>
public sealed class CountryRepository
{
    private readonly DatabaseConnectionService _connectionService;

    public CountryRepository(DatabaseConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IReadOnlyList<CountryDto>> GetActiveCountriesAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, name, shortcode
            FROM dbo.mst_country
            WHERE is_active = 'Y'
            ORDER BY name
            """;

        var result = new List<CountryDto>();

        await using var conn = new SqlConnection(_connectionService.BuildConnectionString());
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new CountryDto(
                Id: (int)reader.GetInt64(0),
                Name: reader.GetString(1),
                Shortcode: reader.GetString(2)
            ));
        }

        return result;
    }

    public async Task<CountryMeta?> GetCountryMetaAsync(int countryId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, name, shortcode,
                   Import_View, Export_View,
                   Import_SP,   Export_SP,
                   TableName
            FROM dbo.mst_country
            WHERE id = @countryId
            """;

        await using var conn = new SqlConnection(_connectionService.BuildConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@countryId", countryId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        return new CountryMeta(
            Id: (int)reader.GetInt64(0),
            Name: reader.GetString(1),
            Shortcode: reader.GetString(2),
            ImportView: reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            ExportView: reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            ImportSP: reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            ExportSP: reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            TableName: reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
        );
    }
}
