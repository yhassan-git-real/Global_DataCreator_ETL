namespace GlobalDataCreatorETL.Core.Models;

public sealed class ValidationResult
{
    private readonly List<string> _errors = new();

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<string> Errors => _errors;
    public string FirstError => _errors.Count > 0 ? _errors[0] : string.Empty;

    public void AddError(string error) => _errors.Add(error);

    public static ValidationResult Success() => new();

    public static ValidationResult Fail(string error)
    {
        var result = new ValidationResult();
        result.AddError(error);
        return result;
    }
}
