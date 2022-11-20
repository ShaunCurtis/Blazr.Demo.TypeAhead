/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

namespace Blazr.Demo.TypeAhead;

public sealed class CountryService
{
    private readonly HttpClient _httpClient;
    private List<CountryData> _baseDataSet = new List<CountryData>();
    private ValueTask _loadTask;

    public IEnumerable<Continent> Continents => _continents;
    public IEnumerable<Country> Countries => _countries;

    private List<Continent> _continents = new();
    private List<Country> _countries = new();

    public CountryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _loadTask = GetBaseData();
    }

    public async ValueTask<IEnumerable<Country>> FilteredCountries(string? searchText, Guid? continentUid = null)
        => await this.GetFilteredCountries(searchText, continentUid);

    private async ValueTask GetBaseData()
    {
        // source country file is https://github.com/samayo/country-json/blob/master/src/country-by-continent.json
        // on my site it's in wwwroot/sample-data/countries.json
        _baseDataSet = await _httpClient.GetFromJsonAsync<List<CountryData>>("sample-data/countries.json") ?? new List<CountryData>();
        var distinctContinentNames = _baseDataSet.Select(item => item.Continent).Distinct().ToList();

        foreach (var continent in distinctContinentNames)
            _continents.Add(new Continent { Name = continent });

        foreach (var continent in _continents)
        {
            var countryNamesInContinent = _baseDataSet.Where(item => item.Continent == continent.Name).Select(item => item.Country).ToList();

            foreach (var countryName in countryNamesInContinent)
                _countries.Add(new Country { Name = countryName, ContinentUid = continent.Uid });
        }
    }

    private async ValueTask<IEnumerable<Country>> GetFilteredCountries(string? searchText, Guid? continentUid = null)
    {
        // Add in a yield to emulate async behaviour;
        await Task.Delay(10);
        if (string.IsNullOrWhiteSpace(searchText))
            return continentUid is null
                ? _countries.OrderBy(item => item.Name).AsEnumerable()
                : _countries.Where(item => item.ContinentUid == continentUid).OrderBy(item => item.Name).AsEnumerable();

        return continentUid is null || continentUid == Guid.Empty
            ? _countries.Where(item => item.Name.ToLower().Contains(searchText.ToLower())).OrderBy(item => item.Name).AsEnumerable()
            : _countries.Where(item => item.Name.ToLower().Contains(searchText.ToLower()) && item.ContinentUid == continentUid).OrderBy(item => item.Name).AsEnumerable();
    }

    private record CountryData
    {
        public required string Country { get; init; }
        public required string Continent { get; init; }
    }

}
