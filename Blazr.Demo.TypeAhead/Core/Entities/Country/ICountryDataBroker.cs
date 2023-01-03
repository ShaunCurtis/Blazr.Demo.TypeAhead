/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

namespace Blazr.Demo.TypeAhead;

public interface ICountryDataBroker
{
    public ValueTask<IEnumerable<Country>> GetCountriesAsync();

    public ValueTask<IEnumerable<Continent>> GetContinentsAsync();

    public ValueTask<IEnumerable<Country>> FilteredCountries(string? searchText, Guid? continentUid = null);

    public ValueTask<IEnumerable<Country>> FilteredCountriesAsync(Guid continentUid);
}
