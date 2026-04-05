using GlobalDataCreatorETL.Core.Models;

namespace GlobalDataCreatorETL.UI.ViewModels;

public sealed partial class MainViewModel
{
    private ValidationResult ValidateInputs()
    {
        if (SelectedCountry is null)
            return ValidationResult.Fail("Please select a country.");

        if (string.IsNullOrWhiteSpace(SelectedSP))
            return ValidationResult.Fail("Stored Procedure is not resolved. Please select a country first.");

        if (string.IsNullOrWhiteSpace(SelectedView))
            return ValidationResult.Fail("View is not resolved. Please select a country first.");

        if (Filter.FromMonthInt <= 0)
            return ValidationResult.Fail("From Month is required.");

        if (Filter.ToMonthInt <= 0)
            return ValidationResult.Fail("To Month is required.");

        if (Filter.FromMonthInt > Filter.ToMonthInt)
            return ValidationResult.Fail("From Month must be earlier than or equal to To Month.");

        var filters = new[] { Filter.HsCode, Filter.Product, Filter.IecCode,
                               Filter.CompanyName, Filter.ForeignCountryCode,
                               Filter.ForeignName, Filter.Port };

        bool hasFilter = filters.Any(f => !string.IsNullOrWhiteSpace(f) && f.Trim() != "%");
        if (!hasFilter)
            return ValidationResult.Fail("At least one filter must be provided (HS Code, Product, IEC, Company, Foreign Country, Foreign Name, or Port).");

        if (string.IsNullOrWhiteSpace(Filter.OutputDirectory))
            return ValidationResult.Fail("Output directory is required. Use the Browse button to select a folder.");

        if (!Directory.Exists(Filter.OutputDirectory))
            return ValidationResult.Fail($"Output directory does not exist: {Filter.OutputDirectory}");

        return ValidationResult.Success();
    }
}
