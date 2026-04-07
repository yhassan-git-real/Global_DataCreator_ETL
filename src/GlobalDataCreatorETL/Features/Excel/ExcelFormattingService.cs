using GlobalDataCreatorETL.Core.Configuration.Models;
using GlobalDataCreatorETL.Core.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace GlobalDataCreatorETL.Features.Excel;

/// <summary>
/// Applies all visual formatting to a worksheet based on ColumnTypeMap and ExcelFormatSettings.
/// </summary>
public sealed class ExcelFormattingService
{
    public void Apply(ExcelWorksheet ws, ColumnTypeMap map, ExcelFormatSettings cfg)    {
        if (ws.Dimension is null) return;

        int totalRows = ws.Dimension.Rows;
        int totalCols = map.TotalColumns;  // Use the actual data column count, not worksheet dimension

        // 1. Global font
        ws.Cells[ws.Dimension.Address].Style.Font.Name = cfg.FontName;
        ws.Cells[ws.Dimension.Address].Style.Font.Size = cfg.FontSize;
        
        // Apply global font color (black for all data)
        if (TryParseColor(cfg.FontColor, out var fontColor))
        {
            ws.Cells[ws.Dimension.Address].Style.Font.Color.SetColor(fontColor);
        }

        // 2. Header row styling
        ApplyHeaderFormatting(ws, totalCols, cfg);

        // 3. Freeze top row
        if (cfg.FreezeTopRow)
            ws.View.FreezePanes(2, 1);

        // 4. Wrap text
        ws.Cells[ws.Dimension.Address].Style.WrapText = cfg.WrapText;

        // 5. Column type formatting
        ApplyColumnFormats(ws, map, cfg, totalRows);

        // 6. Borders (only on data range, not empty columns)
        ApplyBorders(ws, totalRows, totalCols);

        // 7. Column widths
        ApplyColumnWidths(ws, cfg, totalRows, totalCols);
    }

    private static void ApplyHeaderFormatting(ExcelWorksheet ws, int totalCols, ExcelFormatSettings cfg)
    {
        var headerRange = ws.Cells[1, 1, 1, totalCols];
        headerRange.Style.Font.Name = cfg.HeaderFontName;
        headerRange.Style.Font.Size = cfg.HeaderFontSize;
        headerRange.Style.Font.Bold = true;

        if (TryParseColor(cfg.HeaderBackgroundColor, out var bgColor))
        {
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(bgColor);
        }

        // Apply header font color
        if (TryParseColor(cfg.HeaderFontColor, out var fontColor))
        {
            headerRange.Style.Font.Color.SetColor(fontColor);
        }

        // Apply header alignment
        SetHorizontalAlignment(headerRange, cfg.HeaderHorizontalAlignment);
        SetVerticalAlignment(headerRange, cfg.HeaderVerticalAlignment);

        // Set header row height
        ws.Row(1).Height = cfg.HeaderRowHeight;
    }

    private static void ApplyColumnFormats(
        ExcelWorksheet ws, ColumnTypeMap map, ExcelFormatSettings cfg, int totalRows)
    {
        if (totalRows < 2) return; // header only

        foreach (var colIdx in map.DateColumnIndices)
        {
            var col = ws.Cells[2, colIdx, totalRows, colIdx];
            col.Style.Numberformat.Format = cfg.DateFormat;
        }

        foreach (var colIdx in map.NumericColumnIndices)
        {
            var col = ws.Cells[2, colIdx, totalRows, colIdx];
            col.Style.Numberformat.Format = "#,##0.##";
        }

        foreach (var colIdx in map.TextColumnIndices)
        {
            var col = ws.Cells[2, colIdx, totalRows, colIdx];
            col.Style.Numberformat.Format = "@";
        }
    }

    private static void ApplyBorders(ExcelWorksheet ws, int totalRows, int totalCols)
    {
        // Only apply borders to the actual data range, not to empty columns beyond the data
        var range = ws.Cells[1, 1, totalRows, totalCols];
        range.Style.Border.Top.Style    = ExcelBorderStyle.Thin;
        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Left.Style   = ExcelBorderStyle.Thin;
        range.Style.Border.Right.Style  = ExcelBorderStyle.Thin;
    }

    private static void ApplyColumnWidths(ExcelWorksheet ws, ExcelFormatSettings cfg, int totalRows, int totalCols)
    {
        if (!cfg.AutoFitColumns) return;

        // Use fewer sample rows for large datasets, exactly as RapidZ does
        int sampleRows = totalRows > cfg.LargeDatasetThreshold
            ? cfg.AutoFitSampleRowsLarge
            : cfg.AutoFitSampleRows;

        int sampleEndRow = Math.Min(totalRows, sampleRows);
        // Only autofit columns that have data, not beyond the data range
        ws.Cells[1, 1, sampleEndRow, totalCols].AutoFitColumns();
    }

    private static bool TryParseColor(string hex, out Color color)
    {
        try
        {
            color = ColorTranslator.FromHtml(hex);
            return true;
        }
        catch
        {
            color = Color.LightBlue;
            return false;
        }
    }

    private static void SetHorizontalAlignment(ExcelRange range, string alignment)
    {
        range.Style.HorizontalAlignment = alignment?.ToLower() switch
        {
            "left" => ExcelHorizontalAlignment.Left,
            "center" => ExcelHorizontalAlignment.Center,
            "right" => ExcelHorizontalAlignment.Right,
            "justify" => ExcelHorizontalAlignment.Justify,
            "distributed" => ExcelHorizontalAlignment.Distributed,
            _ => ExcelHorizontalAlignment.Center
        };
    }

    private static void SetVerticalAlignment(ExcelRange range, string alignment)
    {
        range.Style.VerticalAlignment = alignment?.ToLower() switch
        {
            "top" => ExcelVerticalAlignment.Top,
            "middle" => ExcelVerticalAlignment.Center,
            "center" => ExcelVerticalAlignment.Center,
            "bottom" => ExcelVerticalAlignment.Bottom,
            "distributed" => ExcelVerticalAlignment.Distributed,
            _ => ExcelVerticalAlignment.Center
        };
    }

    public void ApplySort(ExcelWorksheet ws, ExcelFormatSettings cfg, int totalCols)
    {
        if (ws.Dimension is null) return;
        if (cfg.SortColumns is null || cfg.SortColumns.Count == 0) return;

        int totalRows = ws.Dimension.Rows;
        if (totalRows < 2) return; // header only — nothing to sort

        var validCols = cfg.SortColumns
            .Where(c => c >= 1 && c <= totalCols)
            .ToList();
        if (validCols.Count == 0) return;

        var order = "DESC".Equals(cfg.SortOrder, StringComparison.OrdinalIgnoreCase)
            ? eSortOrder.Descending
            : eSortOrder.Ascending;

        // Sort data rows only (row 2 onward), preserving the header row
        ws.Cells[2, 1, totalRows, totalCols].Sort(x =>
        {
            foreach (var col in validCols)
                x.SortBy.Column(col - 1, order); // EPPlus Sort uses 0-based column index within the range
        });
    }
}
