using GlobalDataCreatorETL.Core.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GlobalDataCreatorETL.Core.Database;

/// <summary>
/// Extracts column schema directly from SqlDataReader metadata.
/// This provides accurate schema based on the actual data source (view or query)
/// rather than relying on a table definition that may differ from the view structure.
/// </summary>
public sealed class SchemaExtractor
{
    /// <summary>
    /// Extracts column information directly from a SqlDataReader's schema table.
    /// This reads the actual columns being returned by the query/view, ensuring
    /// the Excel formatting matches the actual data structure.
    /// </summary>
    public IReadOnlyList<ColumnInfo> ExtractFromDataReader(SqlDataReader reader)
    {
        var result = new List<ColumnInfo>();
        
        if (reader == null || reader.IsClosed)
            return result;

        var schemaTable = reader.GetSchemaTable();
        if (schemaTable == null)
            return result;

        int ordinalPosition = 1;
        foreach (DataRow row in schemaTable.Rows)
        {
            string columnName = row["ColumnName"]?.ToString() ?? $"Column{ordinalPosition}";
            string dataType = GetSqlDataType(row);
            bool isNullable = (bool)(row["AllowDBNull"] ?? true);

            result.Add(new ColumnInfo(
                ColumnName: columnName,
                DataType: dataType,
                OrdinalPosition: ordinalPosition,
                IsNullable: isNullable
            ));

            ordinalPosition++;
        }

        return result;
    }

    /// <summary>
    /// Maps CLR data types from SchemaTable to SQL data type names.
    /// </summary>
    private static string GetSqlDataType(DataRow schemaRow)
    {
        var dataType = schemaRow["DataType"] as Type;
        
        if (dataType == null)
            return "nvarchar";

        // Map CLR types to SQL type names
        return dataType.Name switch
        {
            "DateTime" => "datetime",
            "DateTimeOffset" => "datetimeoffset",
            "Int32" => "int",
            "Int64" => "bigint",
            "Int16" => "smallint",
            "Byte" => "tinyint",
            "Decimal" => "decimal",
            "Double" => "float",
            "Single" => "real",
            "Boolean" => "bit",
            "String" => "nvarchar",
            _ => "nvarchar"
        };
    }
}
