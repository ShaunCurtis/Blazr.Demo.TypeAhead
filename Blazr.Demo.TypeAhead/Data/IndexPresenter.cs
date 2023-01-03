namespace Blazr.Demo.TypeAhead;

public class IndexPresenter
{
    private CountryDataBroker _dataBroker;
    public ValueTask LoadTask => _dataBroker.LoadTask;

    public IndexPresenter(CountryDataBroker countryService)
        => _dataBroker = countryService;

    public string? TypeAheadText;

    public IEnumerable<Country> filteredCountries { get; private set; } = Enumerable.Empty<Country>();

    public async Task<IEnumerable<string>> GetItems(string search)
    {
        var list = await _dataBroker.FilteredCountries(search, null);
        return list.Select(item => item.Name).AsEnumerable();
    }
}
