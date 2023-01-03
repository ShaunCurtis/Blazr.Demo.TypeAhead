global using System.Diagnostics;

using Blazr.Demo.TypeAhead;
using Blazr.Demo.TypeAhead.Data;
using Microsoft.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<CountryDataProvider>();
builder.Services.AddScoped<ICountryDataBroker, CountryDataBroker>();
builder.Services.AddTransient<CountryPresenter>();
builder.Services.AddTransient<IndexPresenter>();
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
