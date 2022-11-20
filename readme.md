# Building a Blazor Autocomplete Control

If you don't want to buy into a component library, you need to build your own.  This article shows you how.

Before we look at the control we need a *De-Bouncer*.  For those unsure what I mean, we need to control the number of component refreshes and calls to the data pipeline caused by keyboard/mouse driven events.

If you type "uni", does the control lookup and refrash the list on every keystroke, or wait until you stop typing?  The user experience will depend on how quickly the control can fetch the data and update the display.  If the data pipeine is slower than typing speed, we build up a queue of requests: there may be perceptible delay while the data pipeline and UI catch up.

*De-bouncing* is a mechanism to minimize this effect.  The normal technique uses a timer which is reset on each keypress and only executes the data pipeline request when the timer expires: often set at 300 millseconds.  It works, but the time taken to update is the timer + the query/refresh period.

## Repos

The repo for this article is here: [Blazr.Demo.TypeAhead](https://github.com/ShaunCurtis/Blazr.Demo.TypeAhead)

## Coding Conventions

1. `Nullable` is enabled globally.  Null error handling relies on it.
2. Net7.0.
3. C# 11.
4. Data objects are immutable: records.
5. `sealed` by default.
6. No underscore global variables in Razor components: VS, not me: it just doesn't seem to like them!

## InputThrottler

This is my de-bouncer.  No timer: it utilizes the built in functionality in the Async library.

The class outline.

```csharp
public sealed class InputThrottler
{
    private int _backOff = 0;
    private Func<Task> _taskToRun;
    private Task _activeTask = Task.CompletedTask;
    private TaskCompletionSource<bool>? _queuedTaskCompletionSource;
    private TaskCompletionSource<bool>? _activeTaskCompletionSource;

    private InputThrottler(Func<Task> toRun, int backOff);
    private async Task RunQueueAsync();
    public Task<bool> QueueAsync();
    public static InputThrottler Create(Func<Task> toRun, int backOff)
            => new InputThrottler(toRun, backOff > 300 ? backOff : 300);
}
``` 

Instantiation is restricted to a static `Create` method. There's no way to just "new" an instance.

The `Func` delegate is the actual method that gets called to refresh the data.  The method pattern is `Task MethodName()`.  

The backoff is the minimum update backoff period: the maximum value is set to 300 milliseconds.

There are two private `TaskCompletionSource` global variables that track the running and queued requests.  If you haven't encountered `TaskCompletionSource` before, it's an object that provides manual creation and management of Tasks.  You'll see how it works in the code.

`_activeTask` references the `Task` for the current instance of `RunQueueAsync`.  It provides a mechanism to check if the queue is currently running or completed.

### QueueAsync

```csharp
public Task<bool> QueueAsync()
{
```

Get a reference to the currently queued CompletionTask.  It will be set to completed once replaced by a new CompletionTask.  It may be null. 

```csharp
    var oldCompletionTask = _queuedTaskCompletionSource;
```

Create a new CompletionTask and get a reference to it's `Task`.  Belt-and-braces stuff to make sure it's referenced before assigned to the active queue. 

```csharp
    var newCompletionTask = new TaskCompletionSource<bool>();
    var task = newCompletionTask.Task;
```
Switch out the CompletionTask reference assigned to the active queue.

```csharp
    // replace _queuedTaskCompletionSource
    _queuedTaskCompletionSource = newCompletionTask;
```

Set the old CompletionTask to completed. Return `false`: nothing happened.
 
```csharp
    if (oldCompletionTask is not null)
        oldCompletionTask?.TrySetResult(false);
```

Is `_activeTask` not completed i.e. `RunQueueAsync` is running.  If not call `RunQueueAsync` and assign it's `Task` reference to `_activeTask`.

```csharp
    if (_activeTask is null || _activeTask.IsCompleted)
        _activeTask = this.RunQueueAsync();
````

Return the task associated with the new queued CompletionTask.

```csharp
    return task;
}
```  

### RunQueueAsync

```csharp
private async Task RunQueueAsync()
{
```

If the current CompletionTask is completed, release the reference to it.

```csharp
    if (_activeTaskCompletionSource is not null && _activeTaskCompletionSource.Task.IsCompleted)
        _activeTaskCompletionSource = null;
```
If the current CompletionTask is running then everything is already in motion and there's nothing to do so return.

```csharp
    if (_activeTaskCompletionSource is not null)
        return;
```

Use a `while` loop to keep the process running while there's a queued CompletionTask. 

```csharp
    while (_queuedTaskCompletionSource is not null)
```

If we're here, there's no active CompletionTask.  Assign a queued CompletionTask reference to the active CompletionTask and release queued CompletionTask reference.  The queue is now empty.

```csharp
        _activeTaskCompletionSource = _queuedTaskCompletionSource;
        _queuedTaskCompletionSource = null;
```

Start a `Task.Delay` task set to delay for the backoff period, the main task in `_taskToRun`, and await both.  The actual backoff period will be the longer running of the two tasks.

```csharp
        var backoffTask = Task.Delay(_backOff);
        var mainTask = _taskToRun.Invoke();
        await Task.WhenAll( new Task[] { mainTask, backoffTask } );
```

The main task has completed so we set the active CompletionTask to completed and release the reference to it.  The return value is true: - we did something.

```csharp
        _activeTaskCompletionSource.TrySetResult(true);
        _activeTaskCompletionSource = null;
    }
```

Loop back to check if another request has been queued: there's been a UI event while we've been processing the last queued request.  If not complete.

```csharp
    return;
}
```

### Summary

The object uses `TaskCompletionSource` instances to represent each request.  It passes the Task associated with the instance of `TaskCompletionSource` back to the caller.  The queued request, represented by the `TaskCompletionSource`, is either:

1. Run by the queue handler.  The task is completed as true: we did something and you probably need to update the UI.
2. Replaced by another request.  It's completed as false: no action needed.

## AutoCompleteComponent

The standard two bind parameters, a `Func` delegate to return a string collection based on a provided string, and the Css to apply to the input.

```csharp
[Parameter] public string? Value { get; set; }
[Parameter] public EventCallback<string?> ValueChanged { get; set; }
[Parameter, EditorRequired] public Func<string?, Task<IEnumerable<string>>>? FilterItems { get; set; }
[Parameter] public string CssClass { get; set; } = "form-control mb-3";
```

The private global variables:

```csharp
private InputThrottler inputThrottler;
private string? filterText;  //The value we'll get from oninput events
private string listid = Guid.NewGuid().ToString(); //unique id for the datalist
private IEnumerable<string> items = Enumerable.Empty<string>(); //string list for the datalist
```

A ctor to initialize the `InputThrottler`.

```csharp
public AutoCompleteControl()
    => inputThrottler = InputThrottler.Create(GetFilteredItems, 300);
```

`OnInitializedAsync` to get the initial filter list.  This may be an empty list.

```csharp
protected override Task OnInitializedAsync()
    => GetFilteredItems();
```
The actual method to get the list items.  If the Parameter `FilterItems` is null set `items` to an empty collection, otherwise set `items` to the returned collection.

```csharp
private async Task GetFilteredItems()
{
    this.Items = FilterItems is null
        ? Enumerable.Empty<string>()
        : await FilterItems.Invoke(filterText);
}
```
Method called by `@oninput`.  It sets `filterText` to the current string and then queues a request on `inputThrottler`.  If this is returned as true - `inputThrottler` didn't *cancel* the request - call `StateHasChanged` to update the component.  See the *Improving the Component Performance* to explain why we call `StateHasChanged`.

```csharp
private async void OnSearchUpdated(ChangeEventArgs e)
{
    this.filterText = e.Value?.ToString() ?? string.Empty;
    if (await inputThrottler.QueueAsync())
        StateHasChanged();
}
```

The UI event handler for an input update invoking the bind `ValueChanged` callback.

```csharp
private Task OnChange(ChangeEventArgs e)
    => this.ValueChanged.InvokeAsync(e.Value?.ToString());
```

The UI markup code:

```html
<input class="@CssClass" type="search" value="@this.Value" @onchange=this.OnChange list="@listid" @oninput=this.OnSearchUpdated />

<datalist id="@listid">
    @foreach (var item in this.Items)
    {
            <option>@item</option>
    }
</datalist>
```
### Improving the Component Performance

The component raises a UI event on every keystroke: `OnSearchUpdated` is called.  As we inherit from `ComponentBase`, this triggers two render events on the component: one before and one after the await yield.  We don't need them: they do nothing unless `inputThrottler.QueueAsync()` returns true.

We can change this by implementing `IHandleEvent` and defining a custom `HandleEventAsync` that just invokes the method with no calls to `StateHasChanged`.  We call it manually when we need to.

We can also *shortcircuit* the `OnAfterRenderAsync` handler as we aren't using it either.

Here';s how to do it:

```csharp
@implements IHandleEvent
@implements IHandleAfterRender

//....
Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    => callback.InvokeAsync(arg);

Task IHandleAfterRender.OnAfterRenderAsync()
    => Task.CompletedTask;
```

Finally we add a code behind file to seal the class: sealed objects are marginally quicker that open objects.  One of the *behind the scenes* changes in .Net7.0 was sealing all classes that could be.

```csharp
public sealed partial class AutoCompleteControl  {}
```

## Demo Page

The code for the data pipeline is in the appendix.  This page demonstrates autocomplete on a country select control.  It's pretty self explanatory.  Either return the whole list if search is empty as done here, or return an empty list.  

```html
@page "/"
@page "/Index"
@inject CountryService Service

<PageTitle>Index</PageTitle>

<AutoCompleteControl FilterItems=this.GetItems @bind-Value=this.typeAheadText />

<div class="alert alert-info">
    typeAheadText : @typeAheadText
</div>
```
```csharp
@code {
    private string? typeAheadText;

    private IEnumerable<Country> filteredCountries = Enumerable.Empty<Country>();

    private async Task<IEnumerable<string>> GetItems(string search)
    {
        var list = await this.Service.FilteredCountries(search, null);
        return list.Select(item => item.Name).AsEnumerable();
    }
}
```

Code behind class:

```csharp
public sealed partial class Index {}
```

## Appendix
The *Data Pipeline* for this article.

### CountryService

```csharp
public sealed class CountryService
{
    private readonly HttpClient _httpClient;
    private List<CountryData> _baseDataSet = new List<CountryData>();
    private ValueTask _loadTask;

    public IEnumerable<Continent> Continents => _continents;
    public IEnumerable<Country> Countries => _countries;

    private List<Continent> _continents = new();
    private List<Country> _countries = new();

    public CountryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _loadTask = GetBaseData();
    }

    public async ValueTask<IEnumerable<Country>> FilteredCountries(string? searchText, Guid? continentUid = null)
        => await this.GetFilteredCountries(searchText, continentUid);

    private async ValueTask GetBaseData()
    {
        // source country file is https://github.com/samayo/country-json/blob/master/src/country-by-continent.json
        // on my site it's in wwwroot/sample-data/countries.json
        _baseDataSet = await _httpClient.GetFromJsonAsync<List<CountryData>>("sample-data/countries.json") ?? new List<CountryData>();
        var distinctContinentNames = _baseDataSet.Select(item => item.Continent).Distinct().ToList();

        foreach (var continent in distinctContinentNames)
            _continents.Add(new Continent { Name = continent });

        foreach (var continent in _continents)
        {
            var countryNamesInContinent = _baseDataSet.Where(item => item.Continent == continent.Name).Select(item => item.Country).ToList();

            foreach (var countryName in countryNamesInContinent)
                _countries.Add(new Country { Name = countryName, ContinentUid = continent.Uid });
        }
    }

    private async ValueTask<IEnumerable<Country>> GetFilteredCountries(string? searchText, Guid? continentUid = null)
    {
        // Add in a yield to emulate async behaviour;
        await Task.Delay(10);
        if (string.IsNullOrWhiteSpace(searchText))
            return continentUid is null
                ? _countries.OrderBy(item => item.Name).AsEnumerable()
                : _countries.Where(item => item.ContinentUid == continentUid).OrderBy(item => item.Name).AsEnumerable();

        return continentUid is null || continentUid == Guid.Empty
            ? _countries.Where(item => item.Name.ToLower().Contains(searchText.ToLower())).OrderBy(item => item.Name).AsEnumerable()
            : _countries.Where(item => item.Name.ToLower().Contains(searchText.ToLower()) && item.ContinentUid == continentUid).OrderBy(item => item.Name).AsEnumerable();
    }
}
```
```csharp
private sealed record CountryData
{
    public required string Country { get; init; }
    public required string Continent { get; init; }
}
```

```csharp
public sealed record Country
{
    public Guid Uid { get; init; } = Guid.NewGuid();
    public required Guid ContinentUid { get; init; }
    public required string Name { get; init; }
}
```
```csharp
public sealed record Continent
{
    public Guid Uid { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
}
```

Services Registration.  This is for Blazor Server

```csharp
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddTransient<CountryService>();
{
    var services = builder.Services;

    // Server Side Blazor doesn't register HttpClient by default
    // Thanks to Robin Sue - Suchiman https://github.com/Suchiman/BlazorDualMode
    if (!services.Any(x => x.ServiceType == typeof(HttpClient)))
    {
        // Setup HttpClient for server side in a client side compatible fashion
        services.AddScoped<HttpClient>(s =>
        {
            // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
            var uriHelper = s.GetRequiredService<NavigationManager>();
            return new HttpClient
            {
                BaseAddress = new Uri(uriHelper.BaseUri)
            };
        });
    }
}
```









