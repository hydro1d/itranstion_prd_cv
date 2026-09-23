using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using CvPlatform.Components;
using CvPlatform.Data;
using CvPlatform.Data.Entities;
using CvPlatform.Endpoints;
using CvPlatform.Resources;

var builder = WebApplication.CreateBuilder(args);

// Configure ASP.NET Core Localization
builder.Services.AddLocalization();

// Configure PostgreSQL with EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure ASP.NET Core Identity & Cookie Authentication
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".CvPlatform.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Configure Blazor Authentication State
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication();

// Domain Services
builder.Services.AddScoped<CvPlatform.Services.IAttributeService, CvPlatform.Services.AttributeService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Run database migration and Identity seeder safely
await IdentityDataSeeder.SeedAsync(app.Services, app.Logger);

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
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Map Account Authentication Endpoints
app.MapAccountEndpoints();

// Language switching endpoint preserving return URL and setting persistent culture cookie
app.MapGet("/api/culture/set", (string culture, string? redirectUri, HttpContext httpContext) =>
{
    if (supportedCultures.Contains(culture))
    {
        httpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions 
            { 
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1), 
                IsEssential = true, 
                SameSite = SameSiteMode.Lax 
            }
        );
    }

    string target = "/";
    if (!string.IsNullOrWhiteSpace(redirectUri))
    {
        if (Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedAbsolute))
        {
            target = parsedAbsolute.PathAndQuery;
        }
        else if (redirectUri.StartsWith('/') && !redirectUri.StartsWith("//"))
        {
            target = redirectUri;
        }
    }

    return Results.LocalRedirect(target);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
