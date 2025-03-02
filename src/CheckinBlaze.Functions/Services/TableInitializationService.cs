using Azure;
using Azure.Data.Tables;
using CheckinBlaze.Shared.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Services
{
    public class TableInitializationService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<TableInitializationService> _logger;
        private readonly HashSet<string> _requiredTables;

        public TableInitializationService(
            TableServiceClient tableServiceClient,
            ILogger<TableInitializationService> logger)
        {
            _tableServiceClient = tableServiceClient;
            _logger = logger;
            
            // Define the set of tables that should exist
            _requiredTables = new HashSet<string>
            {
                AppConstants.TableNames.CheckInRecords,
                AppConstants.TableNames.UserPreferences,
                AppConstants.TableNames.AuditLogs,
                AppConstants.TableNames.HeadcountCampaigns
            };
        }

        public async Task InitializeTablesAsync()
        {
            _logger.LogInformation("Initializing Azure Table Storage tables...");

            try
            {
                // Get list of existing tables
                var existingTables = new HashSet<string>();
                await foreach (var table in _tableServiceClient.QueryAsync())
                {
                    existingTables.Add(table.Name.ToLowerInvariant());
                }

                // Create required tables if they don't exist
                foreach (var tableName in _requiredTables)
                {
                    if (!existingTables.Contains(tableName))
                    {
                        await EnsureTableExistsAsync(tableName);
                    }
                }

                // Clean up any tables that aren't in our required set and aren't system tables
                foreach (var tableName in existingTables)
                {
                    // Skip system tables (like those created by Azure Functions runtime)
                    if (tableName.StartsWith("azure") || tableName.StartsWith("wabs"))
                        continue;

                    // If it's not a required table, delete it
                    if (!_requiredTables.Contains(tableName))
                    {
                        _logger.LogWarning("Found unexpected table {TableName}, removing...", tableName);
                        await _tableServiceClient.DeleteTableAsync(tableName);
                    }
                }
                
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
                _logger.LogInformation("Creating table {TableName}", tableName);
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