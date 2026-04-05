using GlobalDataCreatorETL.Core.Models;

namespace GlobalDataCreatorETL.Core.Services;

/// <summary>
/// Resolves the final file name for the generated Excel file.
/// If the user provides a name it is sanitized; otherwise a name is auto-generated.
/// </summary>
public sealed class FileNameService
{
    private static readonly char[] _invalidChars = Path.GetInvalidFileNameChars();

    public string Resolve(EtlRequest request)
    {
        string name;

        if (!string.IsNullOrWhiteSpace(request.UserFileName))
        {
            name = SanitizeFileName(request.UserFileName.Trim());
            if (!name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                name += ".xlsx";
        }
        else
        {
            name = AutoGenerate(request);
        }

        return EnsureUnique(request.OutputDirectory, name);
    }

    private static string AutoGenerate(EtlRequest request)
    {
        var parts = new[]
        {
            request.HsCode, request.Product, request.CompanyName,
            request.IecCode, request.ForeignCountryCode, request.ForeignName, request.Port
        }
        .Where(v => !string.IsNullOrWhiteSpace(v) && v.Trim() != "%")
        .Select(v => SanitizeFileName(v!.Trim()))
        .ToArray();

        var core = parts.Length > 0 ? string.Join("_", parts) : "ALL";
        var monthRange = BuildMonthRange(request.FromMonth, request.ToMonth);
        var suffix = request.Mode.Equals("Export", StringComparison.OrdinalIgnoreCase) ? "EXP" : "IMP";

        return $"{core}_{monthRange}{suffix}.xlsx";
    }

    private static string BuildMonthRange(int from, int to)
    {
        static string Format(int yyyymm)
        {
            int year  = yyyymm / 100;
            int month = yyyymm % 100;
            string[] months = { "JAN","FEB","MAR","APR","MAY","JUN","JUL","AUG","SEP","OCT","NOV","DEC" };
            return $"{months[month - 1]}{year % 100:D2}";
        }

        return from == to ? Format(from) : $"{Format(from)}-{Format(to)}";
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in _invalidChars)
            name = name.Replace(c, '_');
        return name;
    }

    private static string EnsureUnique(string directory, string fileName)
    {
        var fullPath = Path.Combine(directory, fileName);
        if (!File.Exists(fullPath))
            return fileName;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var timestamp = DateTime.Now.ToString("HHmmss");
        return $"{nameWithoutExt}_{timestamp}{ext}";
    }
}
