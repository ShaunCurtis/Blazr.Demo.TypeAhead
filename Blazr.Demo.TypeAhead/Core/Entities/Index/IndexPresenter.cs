namespace Blazr.Demo.TypeAhead;

public class IndexPresenter
{
    private ICountryDataBroker _dataBroker;

    public IndexPresenter(ICountryDataBroker countryService)
        => _dataBroker = countryService;

    public string? TypeAheadText;

    public IEnumerable<Country> filteredCountries { get; private set; } = Enumerable.Empty<Country>();

    public async Task<IEnumerable<string>> GetItems(string search)
    {
        var list = await _dataBroker.FilteredCountries(search, null);
        return list.Select(item => item.Name).AsEnumerable();
    }
}
