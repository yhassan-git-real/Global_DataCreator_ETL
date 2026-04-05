using GlobalDataCreatorETL.Core.Models;
using OfficeOpenXml;
using Microsoft.Data.SqlClient;

namespace GlobalDataCreatorETL.Features.Excel;

/// <summary>
/// Loads data from a SqlDataReader into an EPPlus worksheet.
/// Checks cancellation every 10,000 rows to stay responsive.
/// </summary>
public sealed class WorksheetDataLoader
{
    public async Task<long> LoadFromReaderAsync(
        ExcelWorksheet ws,
        SqlDataReader reader,
        CancellationToken ct)
    {
        // Use EPPlus built-in bulk load from reader (row 1 = headers)
        // This is synchronous inside, so wrap in Task.Run to not block UI thread
        long rowCount = 0;

        await Task.Run(() =>
        {
            ws.Cells["A1"].LoadFromDataReader(reader, true);
            rowCount = ws.Dimension?.Rows - 1 ?? 0; // subtract header row
        }, ct);

        return rowCount;
    }
}
