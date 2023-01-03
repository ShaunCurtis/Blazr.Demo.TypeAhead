/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

namespace Blazr.Demo.TypeAhead;

public sealed class CountryDataBroker : ICountryDataBroker
{
    private CountryDataProvider _countryDataProvider;

    public CountryDataBroker(CountryDataProvider countryDataProvider)
        => _countryDataProvider = countryDataProvider;

    public async ValueTask<IEnumerable<Country>> GetCountriesAsync()
        => await _countryDataProvider.GetCountriesAsync();

    public async ValueTask<IEnumerable<Continent>> GetContinentsAsync()
        => await _countryDataProvider.GetContinentsAsync();

    public async ValueTask<IEnumerable<Country>> FilteredCountries(string? searchText, Guid? continentUid = null)
        => await _countryDataProvider.FilteredCountries(searchText, continentUid);

    public async ValueTask<IEnumerable<Country>> FilteredCountriesAsync(Guid continentUid)
        => await _countryDataProvider.FilteredCountriesAsync(continentUid);
}
