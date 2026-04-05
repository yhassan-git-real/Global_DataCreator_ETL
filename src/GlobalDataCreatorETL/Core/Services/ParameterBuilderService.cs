using GlobalDataCreatorETL.Core.Models;

namespace GlobalDataCreatorETL.Core.Services;

/// <summary>
/// Builds the SP parameter dictionary from an EtlRequest.
/// Empty or null optional fields are replaced with "%" wildcard.
/// </summary>
public sealed class ParameterBuilderService
{
    public Dictionary<string, object> Build(EtlRequest request)
    {
        var isExport = request.Mode.Equals("Export", StringComparison.OrdinalIgnoreCase);

        return new Dictionary<string, object>
        {
            ["@fromMonth"] = request.FromMonth,
            ["@ToMonth"]   = request.ToMonth,
            ["@hs"]        = Wildcard(request.HsCode),
            ["@prod"]      = Wildcard(request.Product),
            ["@Iec"]       = Wildcard(request.IecCode),
            [isExport ? "@ExpCmp" : "@ImpCmp"] = Wildcard(request.CompanyName),
            ["@forcount"]  = Wildcard(request.ForeignCountryCode),
            ["@forname"]   = Wildcard(request.ForeignName),
            ["@port"]      = Wildcard(request.Port)
        };
    }

    private static string Wildcard(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed == "%" ? "%" : trimmed;
    }
}
