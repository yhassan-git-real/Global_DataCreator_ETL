namespace GlobalDataCreatorETL.Core.Models;

/// <summary>
/// Multi-value input model built from the UI filter before the ETL loop.
/// Each list contains one or more scalar values; a single-element ["%"] means "all".
/// </summary>
public sealed class EtlInputs
{
    public required int CountryId { get; init; }
    public required string CountryName { get; init; }
    public required string Mode { get; init; }          // "Export" | "Import"
    public required string SpName { get; init; }
    public required string ViewName { get; init; }
    public required string TableName { get; init; }

    // Date filters (YYYYMM)
    public required int FromMonth { get; init; }
    public required int ToMonth { get; init; }

    // Multi-value filter lists — each entry is one scalar value; ["%"] means all
    public required List<string> HsCodes { get; init; }
    public required List<string> Products { get; init; }
    public required List<string> IecCodes { get; init; }
    public required List<string> CompanyNames { get; init; }
    public required List<string> ForeignCountryCodes { get; init; }
    public required List<string> ForeignNames { get; init; }
    public required List<string> Ports { get; init; }

    // Output
    public required string OutputDirectory { get; init; }
    /// <summary>Only applied when there is exactly one combination; ignored in batch mode.</summary>
    public string? UserFileName { get; init; }

    /// <summary>Total number of Cartesian combinations across all list dimensions.</summary>
    public int TotalCombinations =>
        HsCodes.Count * Products.Count * IecCodes.Count *
        CompanyNames.Count * ForeignCountryCodes.Count *
        ForeignNames.Count * Ports.Count;
}
