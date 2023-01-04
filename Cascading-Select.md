# Building a Blazor Cascading Select Control

This article demonstrates how to build a cascading Select control.

## CountryPresenter

`CountryPresenter` is the presentation layer object that manages the data used by the UI Page.  It's a `Transient` registered service.

It accesses the data pipeline through the injected `ICountryDataBroker` service.  As the data loading operation is async it provides `LoadTask` for the UI to wait on.

```csharp
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
```

The main markup block sets out two `select` html controls.  The root control implements manual control, the secodary control implements binding. `@this.ContinentOptions()` and `@this.CountryOptions()` are separate render fragments to keep our main block clean and concise.

```csharp
@inject CountryPresenter Presenter

<PageTitle>Country Cascading Select</PageTitle>

<div class="mb-3">
    <label class="form-label">Continent</label>
    <select class="form-select" @onchange=OnContinentChanged>
        @this.ContinentOptions()
    </select>
</div>

<div class="mb-3">
    <label class="form-label">Country</label>
    <select class="form-select" disabled="@this.Presenter.IsCountryDisabled" @bind=this.Presenter.SelectedCountryUid>
        @this.CountryOptions()
    </select>
</div>
```

`ContinentOptions` is defined in the code section.  It's a mixed c# code and markup method.  It loops through the Continents list and marks the current value as selected (if one is selected).

```csharp
    private RenderFragment ContinentOptions() => (__builder) =>
     {
         @this.ShowChoose(this.Presenter.SelectedContinentUid)

         foreach (var continent in this.Presenter.Continents)
         {
             if (continent.Uid == this.Presenter.SelectedContinentUid)
             {
                 <option selected value="@continent.Uid">@continent.Name</option>
             }
             else
             {
                 <option value="@continent.Uid">@continent.Name</option>
             }
         }
     };
```

`CountryOptions` is simpler as it using binding.

```csharp
    private RenderFragment CountryOptions() => (__builder) =>
     {
         @this.ShowChoose(this.Presenter.SelectedCountryUid)

         foreach (var country in this.Presenter.FilteredCountries)
         {
             <option value="@country.Uid">@country.Name</option>
         }
     };
```

`ShowChoose` adds a `Choose...` option if nothing is currently selected.  It will disappear from the select once a value is selected.

```csharp
    private RenderFragment ShowChoose(Guid value) => (__builder) =>
    {
        if (value == Guid.Empty)
        {
            <option value="@Guid.Empty" disabled selected>Choose...</option>
        }
    };
```

The code behind file implements the code.

```csharp
    public sealed partial class CountryPage
    {
        // Waits on the service loading
        // ensures the service data is populated before we try and render it
        protected override async Task OnInitializedAsync()
            => await Presenter.LoadTask;

        private async Task OnContinentChanged(ChangeEventArgs e)
            => await Presenter.UpdateCountryListAsync(e.Value);
    }
```



