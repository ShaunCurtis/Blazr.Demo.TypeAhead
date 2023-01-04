using Microsoft.AspNetCore.Components;

namespace Blazr.Demo.TypeAhead.Pages;

public sealed partial class Index 
{
    protected override async Task OnInitializedAsync()
        => await this.CascadingSelectPresenter.LoadTask;

    private async Task OnContinentChanged(ChangeEventArgs e)
        => await this.CascadingSelectPresenter.UpdateCountryListAsync(e.Value);
}
