using MerdasGold.Features;
using MerdasGold.Features.Authentication;
using MerdasGold.Features.Authentication.Data;
using MerdasGold.Features.Authentication.Entities;
using MerdasGold.Features.Content;
using MerdasGold.Features.Content.Data;
using MerdasGold.Features.Content.Services;
using MerdasGold.Features.Catalog;
using MerdasGold.Features.Catalog.Services;
using MerdasGold.Features.Common.Localization;
using MerdasGold.Features.Persistence;
using MerdasGold.Features.Settings.Services;
using MerdasGold.Features.OperationLogs.Services;
using MerdasGold.Features.Settings;
using MerdasGold.Features.StoreInformation;
using MerdasGold.Features.StoreInformation.Data;
using MerdasGold.Features.StoreInformation.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using MerdasGold.Features.Pricing.Services;
using MerdasGold.Features.Diagnostics.Services;
using MerdasGold.Features.Layout.Storefront;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices();
builder.Services.AddTransient<MudLocalizer, PersianMudLocalizer>();
builder.Services.AddScoped<StorefrontUiState>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContextFactory<MerdasGoldDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<MerdasGoldDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<AdminAccountSeeder>();
builder.Services.AddScoped<UserAdministrationService>();
builder.Services.AddScoped<OperationLogService>();
builder.Services.AddSingleton<SecuritySettingsRuntimeApplier>();
builder.Services.AddScoped<SecuritySettingsService>();
builder.Services.AddScoped<StoreInformationService>();
builder.Services.AddScoped<StoreInformationSeeder>();
builder.Services.AddScoped<ContentManagementService>();
builder.Services.AddScoped<ContentSeeder>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddMemoryCache();
builder.Services.AddDataProtection();
builder.Services.AddHttpClient("taban-gohar", client =>
{
    client.BaseAddress = new Uri("https://webservice.tgnsrv.ir/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.MaxResponseContentBufferSize = 65536;
}).RemoveAllLoggers(); // Provider credentials are in the URL path; never log request URLs.
builder.Services.AddHttpClient("navasan", client =>
{
    client.BaseAddress = new Uri("https://api.navasan.tech/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.MaxResponseContentBufferSize = 65536;
}).RemoveAllLoggers(); // Provider credentials are in its query string; never log request URLs.
builder.Services.AddSingleton<GoldRateService>();
builder.Services.AddScoped<PricingAdminService>();
builder.Services.AddHostedService<GoldRateWorker>();
builder.Services.AddSingleton<ErrorJournal>();
builder.Services.AddSingleton<ILoggerProvider, CircuitErrorLoggerProvider>();
builder.Services.AddHostedService<ErrorJournalWorker>();

var app = builder.Build();

try
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MerdasGoldDbContext>();
    await dbContext.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<StoreInformationSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<SecuritySettingsService>().ApplyCurrentSettingsAsync();
    await scope.ServiceProvider.GetRequiredService<AdminAccountSeeder>().SeedAsync();
}
catch (Exception exception)
{
    var journal = app.Services.GetRequiredService<ErrorJournal>();
    journal.Write(journal.Create(exception, "Startup"));
    throw;
}

app.UseExceptionHandler("/error", createScopeForErrors: true);
app.UseMiddleware<ErrorCaptureMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/status/{0}", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAuthenticationEndpoints();
app.MapSettingsEndpoints();
app.MapStoreInformationEndpoints();
app.MapContentEndpoints();
app.MapCatalogEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
