using CheckinBlaze.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
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
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("checkins"));
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("userpreferences"));
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("headcountcampaigns"));
        services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("auditlogs"));

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

await host.RunAsync();
