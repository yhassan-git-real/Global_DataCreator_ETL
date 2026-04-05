using GlobalDataCreatorETL.Core.DataAccess;
using GlobalDataCreatorETL.Core.Models;

namespace GlobalDataCreatorETL.Core.Services;

public sealed class CountryService
{
    private readonly CountryRepository _repository;
    private List<CountryDto>? _cachedCountries;

    public CountryService(CountryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CountryDto>> GetAllActiveCountriesAsync(CancellationToken ct = default)
    {
        if (_cachedCountries is not null)
            return _cachedCountries;

        var countries = await _repository.GetActiveCountriesAsync(ct);
        _cachedCountries = countries.ToList();
        return _cachedCountries;
    }

    public async Task<CountryMeta?> GetCountryMetaAsync(int countryId, CancellationToken ct = default) =>
        await _repository.GetCountryMetaAsync(countryId, ct);

    public void InvalidateCache() => _cachedCountries = null;
}
