using Azure.Data.Tables;
using CheckinBlaze.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Services
{
    /// <summary>
    /// Service for managing user preferences
    /// </summary>
    public class UserPreferenceService
    {
        private readonly TableClient _preferencesTable;
        private readonly AuditService _auditService;

        public UserPreferenceService(TableClient preferencesTable, AuditService auditService)
        {
            _preferencesTable = preferencesTable;
            _auditService = auditService;
        }

        /// <summary>
        /// Get or create user preferences for the specified user
        /// </summary>
        public async Task<UserPreferences> GetUserPreferencesAsync(string userId)
        {
            // The partition key is always "UserPreferences" and the row key is the user ID
            var response = await _preferencesTable.GetEntityIfExistsAsync<TableEntity>("UserPreferences", userId);
            
            if (response.HasValue)
            {
                return MapEntityToUserPreferences(response.Value);
            }
            else
            {
                // Create default preferences
                var defaultPreferences = new UserPreferences
                {
                    UserId = userId,
                    DefaultLocationPrecision = LocationPrecision.CityWide,
                    EnableLocationServices = true,
                    EnableTeamsNotifications = true,
                    LastModified = DateTimeOffset.UtcNow,
                    LastModifiedBy = userId
                };

                await SaveUserPreferencesAsync(defaultPreferences, userId);
                
                return defaultPreferences;
            }
        }

        /// <summary>
        /// Save user preferences
        /// </summary>
        public async Task<UserPreferences> SaveUserPreferencesAsync(UserPreferences preferences, string requestorId)
        {
            // Get existing preferences if they exist
            var existingResponse = await _preferencesTable.GetEntityIfExistsAsync<TableEntity>("UserPreferences", preferences.UserId);
            string previousState = null;
            
            if (existingResponse.HasValue)
            {
                previousState = JsonSerializer.Serialize(MapEntityToUserPreferences(existingResponse.Value));
            }

            // Update the last modified information
            preferences.LastModified = DateTimeOffset.UtcNow;
            preferences.LastModifiedBy = requestorId;

            // Create a table entity for the preferences
            var entity = new TableEntity
            {
                PartitionKey = "UserPreferences",
                RowKey = preferences.UserId,
                ["DefaultLocationPrecision"] = preferences.DefaultLocationPrecision.ToString(),
                ["EnableLocationServices"] = preferences.EnableLocationServices,
                ["EnableTeamsNotifications"] = preferences.EnableTeamsNotifications,
                ["LastModified"] = preferences.LastModified,
                ["LastModifiedBy"] = preferences.LastModifiedBy
            };

            // Update or insert the entity
            await _preferencesTable.UpsertEntityAsync(entity);

            // Log the action
            await _auditService.LogActionAsync(
                requestorId,
                requestorId == preferences.UserId ? "Self" : "Administrator", 
                existingResponse.HasValue ? AuditActionType.Update : AuditActionType.Create,
                "UserPreferences",
                preferences.UserId,
                previousState,
                JsonSerializer.Serialize(preferences));

            return preferences;
        }

        private UserPreferences MapEntityToUserPreferences(TableEntity entity)
        {
            return new UserPreferences
            {
                UserId = entity.RowKey,
                DefaultLocationPrecision = Enum.Parse<LocationPrecision>(entity.GetString("DefaultLocationPrecision") ?? LocationPrecision.CityWide.ToString()),
                EnableLocationServices = entity.GetBoolean("EnableLocationServices") ?? true,
                EnableTeamsNotifications = entity.GetBoolean("EnableTeamsNotifications") ?? true,
                LastModified = entity.GetDateTimeOffset("LastModified") ?? DateTimeOffset.UtcNow,
                LastModifiedBy = entity.GetString("LastModifiedBy")
            };
        }
    }
}