using Microsoft.AspNetCore.Components;

namespace Blazr.Demo.TypeAhead.Pages
{
    public sealed partial class CountryPage
    {
        // Waits on the service loading
        // ensures the service data is populated before we try and render it
        protected override async Task OnInitializedAsync()
            => await Presenter.LoadTask;

        private async Task OnContinentChanged(ChangeEventArgs e)
            => await Presenter.UpdateCountryListAsync(e.Value);
    }
}
