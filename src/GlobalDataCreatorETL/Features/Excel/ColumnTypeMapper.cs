using GlobalDataCreatorETL.Core.Models;
using OfficeOpenXml;

namespace GlobalDataCreatorETL.Features.Excel;

/// <summary>
/// Maps SQL column data types to Excel format categories (date/numeric/text).
/// 1-based column indices matching the EPPlus worksheet column positions.
/// </summary>
public sealed class ColumnTypeMapper
{
    private static readonly HashSet<string> _dateTypes = new(StringComparer.OrdinalIgnoreCase)
        { "datetime", "datetime2", "date", "smalldatetime", "datetimeoffset" };

    private static readonly HashSet<string> _numericTypes = new(StringComparer.OrdinalIgnoreCase)
        { "int", "bigint", "smallint", "tinyint", "decimal", "numeric", "float", "real", "money", "smallmoney", "bit" };

    private static readonly HashSet<string> _dateColumnNames = new(StringComparer.OrdinalIgnoreCase)
        { "import_date", "export_date", "assessment_date", "registration_date", "date", "month" };

    public ColumnTypeMap MapColumns(IReadOnlyList<ColumnInfo> schema)
    {
        var map = new ColumnTypeMap { TotalColumns = schema.Count };

        foreach (var col in schema)
        {
            int colIndex = col.OrdinalPosition; // 1-based from DB

            // Check if it's a date column by type or name
            bool isDateColumn = _dateTypes.Contains(col.DataType) || IsDateColumnName(col.ColumnName);

            if (isDateColumn)
                map.DateColumnIndices.Add(colIndex);
            else if (_numericTypes.Contains(col.DataType))
                map.NumericColumnIndices.Add(colIndex);
            else
                map.TextColumnIndices.Add(colIndex);
        }

        return map;
    }

    private static bool IsDateColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return false;

        // Check exact matches first
        if (_dateColumnNames.Contains(columnName)) return true;

        // Check if name contains date-related keywords
        var lowerName = columnName.ToLower();
        return lowerName.Contains("date") || lowerName.Contains("month") || 
               lowerName.Contains("_date") || lowerName.EndsWith("_date");
    }
}
