using GlobalDataCreatorETL.Core.Models;

namespace GlobalDataCreatorETL.Core.Services;

public sealed class ValidationService
{
    public ValidationResult ValidateRequest(EtlRequest request)
    {
        var result = new ValidationResult();

        // Date validation
        if (request.FromMonth <= 0)
            result.AddError("From Month is required.");

        if (request.ToMonth <= 0)
            result.AddError("To Month is required.");

        if (request.FromMonth > 0 && request.ToMonth > 0 && request.FromMonth > request.ToMonth)
            result.AddError("From Month must be earlier than or equal to To Month.");

        // At least one filter required
        var filters = new[] { request.HsCode, request.Product, request.IecCode,
                               request.CompanyName, request.ForeignCountryCode,
                               request.ForeignName, request.Port };

        bool hasFilter = filters.Any(f => !string.IsNullOrWhiteSpace(f) && f.Trim() != "%");
        if (!hasFilter)
            result.AddError("At least one filter (HS Code, Product, IEC, Company, Foreign Country, Foreign Name, or Port) must be provided.");

        // DB selections
        if (string.IsNullOrWhiteSpace(request.SpName))
            result.AddError("Stored Procedure must be selected.");

        if (string.IsNullOrWhiteSpace(request.ViewName))
            result.AddError("View must be selected.");

        // Output path
        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
            result.AddError("Output directory is required.");
        else if (!Directory.Exists(request.OutputDirectory))
            result.AddError($"Output directory does not exist: {request.OutputDirectory}");

        return result;
    }
}
