namespace GlobalDataCreatorETL.Core.Models;

public sealed class ColumnTypeMap
{
    public List<int> DateColumnIndices { get; } = new();
    public List<int> NumericColumnIndices { get; } = new();
    public List<int> TextColumnIndices { get; } = new();
    public int TotalColumns { get; set; }
}
