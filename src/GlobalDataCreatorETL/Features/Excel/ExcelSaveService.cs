using OfficeOpenXml;

namespace GlobalDataCreatorETL.Features.Excel;

/// <summary>
/// Saves an EPPlus ExcelPackage to disk.
/// Small files (<50k rows) use MemoryStream for speed.
/// Large files write directly to FileInfo to avoid OOM.
/// </summary>
public sealed class ExcelSaveService
{
    private const int MemoryStreamThreshold = 50_000;

    public async Task SaveAsync(ExcelPackage pkg, string outputPath, long rowCount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        if (rowCount < MemoryStreamThreshold)
        {
            await using var ms = new MemoryStream();
            await pkg.SaveAsAsync(ms);
            await File.WriteAllBytesAsync(outputPath, ms.ToArray());
        }
        else
        {
            await pkg.SaveAsAsync(new FileInfo(outputPath));
        }
    }
}
