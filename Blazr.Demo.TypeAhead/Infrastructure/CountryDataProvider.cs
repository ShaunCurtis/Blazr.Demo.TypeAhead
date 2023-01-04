/// ============================================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: Use And Donate
/// If you use it, donate something to a charity somewhere
/// ============================================================

namespace Blazr.Demo.TypeAhead;

public sealed class CountryDataProvider
{
    private readonly HttpClient _httpClient;
    private List<CountryData> _baseDataSet = new List<CountryData>();
    public Task LoadTask { get; private set; } = Task.CompletedTask;

    private List<Continent> _continents = new();
    private List<Country> _countries = new();

    public CountryDataProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        this.LoadTask = LoadBaseData();
    }

    public async ValueTask<IEnumerable<Country>> GetCountriesAsync()
    {
        await this.LoadTask;
        return _countries.AsEnumerable();
    }

    public async ValueTask<IEnumerable<Continent>> GetContinentsAsync()
    {
        await this.LoadTask;
        return _continents.AsEnumerable();
    }

    public async ValueTask<IEnumerable<Country>> FilteredCountries(string? searchText, Guid? continentUid = null)
        => await this.GetFilteredCountries(searchText, continentUid);

    public async ValueTask<IEnumerable<Country>> FilteredCountriesAsync(Guid continentUid)
    {
        await this.LoadTask;
        return _countries.Where(item => item.ContinentUid == continentUid);
    }

    private async Task LoadBaseData()
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
        await this.LoadTask;

        var query = _countries.AsEnumerable();

        if (continentUid is not null && continentUid != Guid.Empty)
            query = query.Where(item => item.ContinentUid == continentUid);

        if (!string.IsNullOrWhiteSpace(searchText))
            query = query.Where(item => item.Name.ToLower().Contains(searchText.ToLower()));

        return query.OrderBy(item => item.Name);
    }

    private record CountryData
    {
        public required string Country { get; init; }
        public required string Continent { get; init; }
    }
}
