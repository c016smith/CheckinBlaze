using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CheckinBlaze.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using CheckinBlaze.Shared.Constants;
using Microsoft.Graph;
using CheckinBlaze.Client.Services;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configure base address for backend API (Azure Functions)
var functionsBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7071";

// Configure Microsoft Authentication Library (MSAL) for Azure AD authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("api://4b781c1c-0b04-4c3b-ad66-427458e9f98d/user_impersonation");
    options.ProviderOptions.AdditionalScopesToConsent.Add($"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserRead}");
    options.ProviderOptions.AdditionalScopesToConsent.Add($"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserReadBasicAll}");
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Cache.CacheLocation = "sessionStorage";
});

// Configure HTTP client for Functions API with direct access (no auth required for now)
builder.Services.AddHttpClient("CheckinBlazeFunctions", client => 
{
    client.BaseAddress = new Uri(functionsBaseUrl);
});

// Configure Microsoft Graph client
builder.Services.AddScoped<GraphServiceClient>(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(
            authorizedUrls: new[] { AppConstants.GraphApi.BaseUrl },
            scopes: new[] { 
                $"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserRead}",
                $"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserReadBasicAll}"
            });

    return new GraphServiceClient(new HttpClient(handler));
});

// Register services
builder.Services.AddScoped<GraphService>();
builder.Services.AddScoped<CheckInService>();
builder.Services.AddScoped<HeadcountService>();

await builder.Build().RunAsync();
