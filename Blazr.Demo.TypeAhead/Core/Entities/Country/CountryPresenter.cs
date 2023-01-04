namespace Blazr.Demo.TypeAhead;

public class CountryPresenter
{
    private ICountryDataBroker _dataBroker;
    public IEnumerable<Country> FilteredCountries { get; private set; } = Enumerable.Empty<Country>();
    public IEnumerable<Continent> Continents { get; private set; } = Enumerable.Empty<Continent>();
    public Guid SelectedCountryUid { get; set; }
    public Guid SelectedContinentUid { get; set; }
    public bool IsCountryDisabled => SelectedContinentUid == Guid.Empty;

    public Task LoadTask = Task.CompletedTask;

    public CountryPresenter(ICountryDataBroker countryService)
    { 
        _dataBroker = countryService;
        LoadTask = this.LoadData();
    }

    private async Task LoadData()
        => this.Continents = await _dataBroker.GetContinentsAsync();

    public async ValueTask<bool> UpdateCountryListAsync(object? id)
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
