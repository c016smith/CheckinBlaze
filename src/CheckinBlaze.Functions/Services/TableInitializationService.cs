using Azure;
using Azure.Data.Tables;
using CheckinBlaze.Shared.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Services
{
    public class TableInitializationService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<TableInitializationService> _logger;

        public TableInitializationService(
            TableServiceClient tableServiceClient,
            ILogger<TableInitializationService> logger)
        {
            _tableServiceClient = tableServiceClient;
            _logger = logger;
        }

        public async Task InitializeTablesAsync()
        {
            _logger.LogInformation("Initializing Azure Table Storage tables...");

            try
            {
                // Create all required tables if they don't exist
                await EnsureTableExistsAsync(AppConstants.TableNames.CheckInRecords.ToLower());
                await EnsureTableExistsAsync(AppConstants.TableNames.UserPreferences.ToLower());
                await EnsureTableExistsAsync(AppConstants.TableNames.AuditLogs.ToLower());
                await EnsureTableExistsAsync(AppConstants.TableNames.HeadcountCampaigns.ToLower());
                
                _logger.LogInformation("Table initialization complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Azure Storage tables");
                throw;
            }
        }

        private async Task EnsureTableExistsAsync(string tableName)
        {
            try
            {
                _logger.LogInformation("Checking if table {TableName} exists", tableName);
                await _tableServiceClient.CreateTableIfNotExistsAsync(tableName);
                _logger.LogInformation("Table {TableName} is ready", tableName);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // Table already exists
                _logger.LogInformation("Table {TableName} already exists", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table {TableName}", tableName);
                throw;
            }
        }
    }
}