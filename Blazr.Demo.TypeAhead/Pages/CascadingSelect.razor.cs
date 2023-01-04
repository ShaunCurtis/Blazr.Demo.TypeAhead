using Microsoft.AspNetCore.Components;

namespace Blazr.Demo.TypeAhead.Pages;

public sealed partial class CascadingSelect
{
    public bool _isCountryDisabled => this.Presenter.SelectedContinentUid == Guid.Empty;

    // Waits on the service loading
    // ensures the service data is populated before we try and render it
    protected override async Task OnInitializedAsync()
        => await this.Presenter.LoadTask;

    private async Task OnContinentChanged(ChangeEventArgs e)
        => await this.Presenter.UpdateCountryListAsync(e.Value);
}
