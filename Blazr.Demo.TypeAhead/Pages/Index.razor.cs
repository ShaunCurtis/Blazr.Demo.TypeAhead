using Microsoft.AspNetCore.Components;

namespace Blazr.Demo.TypeAhead.Pages;

public sealed partial class Index 
{
    private bool _isCountryDisabled => this.CascadingSelectPresenter.SelectedContinentUid == Guid.Empty;

    protected override async Task OnInitializedAsync()
        => await this.CascadingSelectPresenter.LoadTask;

    private async Task OnContinentChanged(ChangeEventArgs e)
        => await this.CascadingSelectPresenter.UpdateCountryListAsync(e.Value);
}
