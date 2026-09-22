using System.Globalization;
using Microsoft.AspNetCore.Localization;
using CvPlatform.Components;

var builder = WebApplication.CreateBuilder(args);

// Configure ASP.NET Core Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure Localization Middleware
var supportedCultures = new[] { "en", "bn" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

// Language switching endpoint preserving return URL and setting persistent culture cookie
app.MapGet("/api/culture/set", (string culture, string? redirectUri, HttpContext httpContext) =>
{
    if (supportedCultures.Contains(culture))
    {
        httpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax }
        );
    }

    var target = string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri;
    return Results.LocalRedirect(target);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
