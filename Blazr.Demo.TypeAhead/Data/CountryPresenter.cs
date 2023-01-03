namespace Blazr.Demo.TypeAhead;

public class CountryPresenter
{
    private CountryDataBroker _dataBroker;
    public IEnumerable<Country> FilteredCountries { get; private set; } = Enumerable.Empty<Country>();
    public Guid SelectedCountryUid { get; set; }
    public Guid SelectedContinentUid { get; set; }
    public bool IsCountryDisabled => SelectedContinentUid == Guid.Empty;
    public ValueTask LoadTask => _dataBroker.LoadTask;

    public CountryPresenter(CountryDataBroker countryService)
        => _dataBroker = countryService;

    public IEnumerable<Continent> Continents => _dataBroker.Continents;

    public async Task<bool> UpdateCountryListAsync(object? id)
    {
        if (Guid.TryParse(id?.ToString() ?? string.Empty, out Guid value))
        {
            SelectedContinentUid = value;

            SelectedCountryUid = Guid.Empty;
            this.FilteredCountries = await _dataBroker.FilteredCountriesAsync(SelectedContinentUid);
            return true;
        }
        return false;
    }
}
