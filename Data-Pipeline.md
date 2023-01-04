# The Data Pipeline for the Solution

The *Data Pipeline* for these articles.  It may appear a little complex, but it's based on *Clean Design*. 

### CountryDataProvider

`CountryDataProvider` gets the data from the API and maps it into application data objects.  It's an infrastructure domain object.

The provider gets the data from the API when it loads.  As this is an async operation it uses `LoadTask` to hold the executing background API load code and awaits it's completion on any data requests.

```csharp
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
```

### CountryDataBroker

An interface and an implementation that uses the `CountryDataProvider`.

```csharp
public interface ICountryDataBroker
{
    public ValueTask<IEnumerable<Country>> GetCountriesAsync();
    public ValueTask<IEnumerable<Continent>> GetContinentsAsync();
    public ValueTask<IEnumerable<Country>> FilteredCountries(string? searchText, Guid? continentUid = null);
    public ValueTask<IEnumerable<Country>> FilteredCountriesAsync(Guid continentUid);
}
```

```csharp
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
```

```csharp
public sealed record Country
{
    public Guid Uid { get; init; } = Guid.NewGuid();
    public required Guid ContinentUid { get; init; }
    public required string Name { get; init; }
}
```
```csharp
public sealed record Continent
{
    public Guid Uid { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
}
```

Services Registration.  This is for Blazor Server.

```csharp
// Add services to the container.
builder.Services.AddScoped<CountryDataProvider>();
builder.Services.AddScoped<ICountryDataBroker, CountryDataBroker>();
builder.Services.AddTransient<CountryPresenter>();
builder.Services.AddTransient<IndexPresenter>();
if (!builder.Services.Any(x => x.ServiceType == typeof(HttpClient)))
{
    builder.Services.AddScoped<HttpClient>(s =>
    {
        var uriHelper = s.GetRequiredService<NavigationManager>();
        return new HttpClient { BaseAddress = new Uri(uriHelper.BaseUri) };
    });
}
```
