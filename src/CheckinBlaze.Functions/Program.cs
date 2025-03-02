using CheckinBlaze.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;
using CheckinBlaze.Functions.Middleware;
using Microsoft.Extensions.Logging;
using System;
using CheckinBlaze.Shared.Constants;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerBuilder => 
    {
        // Add our custom JWT authentication middleware to the request pipeline
        workerBuilder.UseMiddleware<JwtAuthenticationMiddleware>();
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Add Azure AD authentication
        services.AddMicrosoftIdentityWebApiAuthentication(context.Configuration);

        // Add a mock IDownstreamApi for test endpoints
        services.AddScoped<IDownstreamApi, MockDownstreamApi>();

        // Configure Azure Table Storage
        var tableConnectionString = context.Configuration["AzureTableStorageConnection"];
        if (string.IsNullOrEmpty(tableConnectionString))
        {
            throw new ArgumentNullException("connectionString", "AzureTableStorageConnection is not set in the configuration.");
        }
        services.AddSingleton(new TableServiceClient(tableConnectionString));
        
        // Add the TableInitializationService
        services.AddSingleton<TableInitializationService>();
        
        // Register table clients with lowercase table names for Azurite compatibility
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient(AppConstants.TableNames.CheckInRecords));
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient(AppConstants.TableNames.UserPreferences));
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient(AppConstants.TableNames.HeadcountCampaigns));
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient(AppConstants.TableNames.AuditLogs));

        // Register application services in dependency order
        services.AddScoped<AuditService>();
        services.AddScoped<CheckInService>();
        services.AddScoped<UserPreferenceService>();
        services.AddScoped<HeadcountService>();

        // Add Authorization and Routing services
        services.AddAuthorization();
        services.AddRouting();

        // Add CORS policies
        services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", builder =>
            {
                builder
                    .WithOrigins(context.Configuration["AllowedOrigins"]?.Split(',') ?? Array.Empty<string>())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    })
    .Build();

// Initialize Azure Table Storage tables
var logger = host.Services.GetRequiredService<ILogger<Program>>();
try
{
    logger.LogInformation("Initializing application tables...");
    var tableInitService = host.Services.GetRequiredService<TableInitializationService>();
    tableInitService.InitializeTablesAsync().GetAwaiter().GetResult();
    logger.LogInformation("Table initialization completed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error initializing tables. Application may not function correctly.");
}

await host.RunAsync();
