namespace GlobalDataCreatorETL.Core.Models;

public sealed record ColumnInfo(
    string ColumnName,
    string DataType,
    int OrdinalPosition,
    bool IsNullable
);
