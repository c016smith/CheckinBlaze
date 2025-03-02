using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CheckinBlaze.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using CheckinBlaze.Shared.Constants;
using Microsoft.Graph;
using CheckinBlaze.Client.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

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
    options.ProviderOptions.DefaultAccessTokenScopes.Add(AuthorizationConstants.Scopes.AppAccess);
    options.ProviderOptions.AdditionalScopesToConsent.Add($"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserRead}");
    options.ProviderOptions.AdditionalScopesToConsent.Add($"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserReadBasicAll}");
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Cache.CacheLocation = "sessionStorage";
});

// Configure authorization policies
builder.Services.AddAuthorizationCore(options =>
{
    // Add a simple authenticated user policy that doesn't require specific roles
    options.AddPolicy(AuthorizationConstants.Policies.AuthenticatedUser, 
        policy => policy.RequireAuthenticatedUser());
});

// Configure HTTP client for Functions API with authentication
builder.Services.AddHttpClient("CheckinBlazeFunctions", client => 
{
    client.BaseAddress = new Uri(functionsBaseUrl);
})
.AddHttpMessageHandler(sp => 
{
    return sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(
            authorizedUrls: new[] { functionsBaseUrl },
            scopes: new[] { AuthorizationConstants.Scopes.AppAccess });
});

// Configure HTTP client for Microsoft Graph with authentication
builder.Services.AddHttpClient("GraphClient", client =>
{
    // Fix: Use BaseApiUrl instead of BaseUrl to include the API version
    client.BaseAddress = new Uri(AppConstants.GraphApi.BaseApiUrl);
})
.AddHttpMessageHandler(sp =>
{
    return sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(
            authorizedUrls: new[] { AppConstants.GraphApi.BaseUrl },
            scopes: new[] { 
                $"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserRead}",
                $"{AppConstants.GraphApi.BaseUrl}/{AppConstants.ApiScopes.UserReadBasicAll}"
            });
});

// Configure Microsoft Graph client
builder.Services.AddScoped<GraphServiceClient>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>()
        .CreateClient("GraphClient");
    
    return new GraphServiceClient(httpClient);
});

// Register services
builder.Services.AddScoped<GraphService>();
builder.Services.AddScoped<CheckInService>();
builder.Services.AddScoped<HeadcountService>();

await builder.Build().RunAsync();
