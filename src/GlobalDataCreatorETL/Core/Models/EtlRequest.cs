namespace GlobalDataCreatorETL.Core.Models;

public sealed class EtlRequest
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

    // Optional filters — null/empty is replaced with "%" before SP execution
    public string? HsCode { get; init; }
    public string? Product { get; init; }
    public string? IecCode { get; init; }
    public string? CompanyName { get; init; }
    public string? ForeignCountry { get; init; }
    public string? ForeignName { get; init; }
    public string? Port { get; init; }

    // Output
    public required string OutputDirectory { get; init; }
    public string? UserFileName { get; init; }
}
