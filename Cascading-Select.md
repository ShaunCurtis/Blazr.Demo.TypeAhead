# Building a Blazor Cascading Select Control

This article demonstrates how to build a cascading Select control.

## Repos

The repo for this article is here: [Blazr.Demo.TypeAhead](https://github.com/ShaunCurtis/Blazr.Demo.TypeAhead)

## Coding Conventions

1. `Nullable` is enabled globally.  Null error handling relies on it.
2. Net7.0.
3. C# 10.
4. Data objects are immutable: records.
5. `sealed` by default.

## Structure

The code is structured using Clean Design principles.

## Data

The demonstration data for this article is a Continent/Country data set. The source code file is [https://github.com/samayo/country-json/blob/master/src/country-by-continent.json](https://github.com/samayo/country-json/blob/master/src/country-by-continent.json).

The CountryService and data classes are detailed in the Appendix.

The data used by the UI is managed by a presenter servicve in the application layer.  This is a Transient Service that injects the `CountryService` data provider and holds the data fields and collections.

```csharp
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



