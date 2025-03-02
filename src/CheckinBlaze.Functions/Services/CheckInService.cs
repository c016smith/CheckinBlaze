using Azure.Data.Tables;
using CheckinBlaze.Functions.Models;
using CheckinBlaze.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Services
{
    /// <summary>
    /// Service for managing check-in operations
    /// </summary>
    public class CheckInService
    {
        private readonly TableClient _checkInTable;
        private readonly AuditService _auditService;

        public CheckInService(
            TableClient checkInTable,
            AuditService auditService)
        {
            _checkInTable = checkInTable;
            _auditService = auditService;
        }

        /// <summary>
        /// Create a new check-in record
        /// </summary>
        public async Task<CheckInRecord> CreateCheckInAsync(CheckInRecord checkIn, string requestorId)
        {
            // Ensure the request is valid
            if (string.IsNullOrEmpty(checkIn.UserId))
            {
                throw new ArgumentException("User ID is required", nameof(checkIn));
            }

            // Generate a new ID if one wasn't provided
            if (string.IsNullOrEmpty(checkIn.Id))
            {
                checkIn.Id = Guid.NewGuid().ToString();
            }

            // Set the timestamp to now if one wasn't provided
            if (checkIn.Timestamp == default)
            {
                checkIn.Timestamp = DateTimeOffset.UtcNow;
            }

            // Map to a table entity and add to the checkinrecords table
            var entity = CheckInEntity.FromModel(checkIn);
            await _checkInTable.AddEntityAsync(entity);

            // Log the action in the audit log
            await _auditService.LogActionAsync(
                requestorId,
                checkIn.UserDisplayName,
                AuditActionType.CheckIn,
                "CheckInRecord",
                checkIn.Id,
                null,
                System.Text.Json.JsonSerializer.Serialize(checkIn));

            return checkIn;
        }

        /// <summary>
        /// Get a check-in by ID
        /// </summary>
        public async Task<CheckInRecord> GetCheckInAsync(string userId, string checkInId)
        {
            var response = await _checkInTable.GetEntityIfExistsAsync<CheckInEntity>(userId, checkInId);
            
            if (response.HasValue)
            {
                return response.Value.ToModel();
            }
            
            return null;
        }

        /// <summary>
        /// Get the most recent check-in for a user
        /// </summary>
        public async Task<CheckInRecord> GetLatestCheckInAsync(string userId)
        {
            // Query for the user's check-ins, ordered by timestamp descending
            var query = _checkInTable.QueryAsync<CheckInEntity>(filter: $"PartitionKey eq '{userId}'");

            var checkIns = new List<CheckInEntity>();
            
            await foreach (var page in query.AsPages())
            {
                checkIns.AddRange(page.Values);
            }

            var latestCheckIn = checkIns.OrderByDescending(c => c.CheckInTimestamp).FirstOrDefault();
            
            if (latestCheckIn != null)
            {
                return latestCheckIn.ToModel();
            }
            
            return null;
        }

        /// <summary>
        /// Get check-in history for a user
        /// </summary>
        public async Task<List<CheckInRecord>> GetCheckInHistoryAsync(string userId, int maxResults = 50)
        {
            // Query for all check-ins in the last 30 days by default
            var startDate = DateTimeOffset.UtcNow.AddDays(-30);
            var filter = $"PartitionKey eq '{userId}' and CheckInTimestamp ge datetime'{startDate:yyyy-MM-ddTHH:mm:ssZ}'";
            var query = _checkInTable.QueryAsync<CheckInEntity>(filter);

            var checkIns = new List<CheckInEntity>();
            
            await foreach (var page in query.AsPages())
            {
                checkIns.AddRange(page.Values);
                if (checkIns.Count >= maxResults)
                {
                    break;
                }
            }

            return checkIns
                .OrderByDescending(c => c.CheckInTimestamp)
                .Take(maxResults)
                .Select(c => c.ToModel())
                .ToList();
        }

        /// <summary>
        /// Update an existing check-in
        /// </summary>
        public async Task<CheckInRecord> UpdateCheckInAsync(CheckInRecord checkIn, string requestorId)
        {
            if (string.IsNullOrEmpty(checkIn.Id) || string.IsNullOrEmpty(checkIn.UserId))
            {
                throw new ArgumentException("Check-in ID and User ID are required");
            }

            // Get the existing check-in
            var existingResponse = await _checkInTable.GetEntityIfExistsAsync<CheckInEntity>(checkIn.UserId, checkIn.Id);
            
            if (!existingResponse.HasValue)
            {
                throw new KeyNotFoundException($"Check-in with ID {checkIn.Id} not found");
            }

            var existingEntity = existingResponse.Value;
            var previousState = existingEntity.ToModel();

            // Update only the properties that can be updated
            existingEntity.Notes = checkIn.Notes;
            existingEntity.Status = checkIn.Status.ToString();
            existingEntity.State = checkIn.State.ToString();
            existingEntity.AcknowledgedByUserId = checkIn.AcknowledgedByUserId;
            existingEntity.AcknowledgedTimestamp = checkIn.AcknowledgedTimestamp;
            existingEntity.ResolvedByUserId = checkIn.ResolvedByUserId;
            existingEntity.ResolvedTimestamp = checkIn.ResolvedTimestamp;

            // Update in the table
            await _checkInTable.UpdateEntityAsync(existingEntity, existingEntity.ETag);

            var updatedCheckIn = existingEntity.ToModel();

            // Log the action
            await _auditService.LogActionAsync(
                requestorId,
                "System",
                AuditActionType.Update,
                "CheckInRecord",
                checkIn.Id,
                System.Text.Json.JsonSerializer.Serialize(previousState),
                System.Text.Json.JsonSerializer.Serialize(updatedCheckIn));

            return updatedCheckIn;
        }

        /// <summary>
        /// Acknowledge a check-in that needs assistance
        /// </summary>
        public async Task<CheckInRecord> AcknowledgeCheckInAsync(string userId, string checkInId, string acknowledgedByUserId, string acknowledgedByDisplayName)
        {
            var checkIn = await GetCheckInAsync(userId, checkInId);
            
            if (checkIn == null)
            {
                throw new KeyNotFoundException($"Check-in with ID {checkInId} not found");
            }
            
            if (checkIn.Status != SafetyStatus.NeedsAssistance)
            {
                throw new InvalidOperationException("Can only acknowledge check-ins that need assistance");
            }
            
            if (checkIn.State != CheckInState.Submitted)
            {
                throw new InvalidOperationException("Check-in has already been acknowledged or resolved");
            }
            
            checkIn.State = CheckInState.Acknowledged;
            checkIn.AcknowledgedByUserId = acknowledgedByUserId;
            checkIn.AcknowledgedTimestamp = DateTimeOffset.UtcNow;

            return await UpdateCheckInAsync(checkIn, acknowledgedByUserId);
        }

        /// <summary>
        /// Resolve a check-in that has been acknowledged
        /// </summary>
        public async Task<CheckInRecord> ResolveCheckInAsync(string userId, string checkInId, string resolvedByUserId, string resolvedByDisplayName)
        {
            var checkIn = await GetCheckInAsync(userId, checkInId);
            
            if (checkIn == null)
            {
                throw new KeyNotFoundException($"Check-in with ID {checkInId} not found");
            }
            
            if (checkIn.State != CheckInState.Acknowledged)
            {
                throw new InvalidOperationException("Can only resolve check-ins that have been acknowledged");
            }

            checkIn.State = CheckInState.Resolved;
            checkIn.ResolvedByUserId = resolvedByUserId;
            checkIn.ResolvedTimestamp = DateTimeOffset.UtcNow;

            return await UpdateCheckInAsync(checkIn, resolvedByUserId);
        }

        /// <summary>
        /// Get all check-ins that need assistance and have not been resolved
        /// </summary>
        public async Task<List<CheckInRecord>> GetNeedsAssistanceCheckInsAsync()
        {
            // Filter for all check-ins with Status = "NeedsAssistance" and State != "Resolved"
            var filter = $"Status eq 'NeedsAssistance' and State ne 'Resolved'";
            var query = _checkInTable.QueryAsync<CheckInEntity>(filter: filter);

            var checkIns = new List<CheckInEntity>();
            
            await foreach (var page in query.AsPages())
            {
                checkIns.AddRange(page.Values);
            }

            return checkIns
                .OrderByDescending(c => c.CheckInTimestamp)
                .Select(c => c.ToModel())
                .ToList();
        }

        /// <summary>
        /// Get check-ins associated with a specific headcount campaign
        /// </summary>
        public async Task<List<CheckInRecord>> GetCheckInsByCampaignAsync(string campaignId)
        {
            var filter = $"HeadcountCampaignId eq '{campaignId}'";
            var query = _checkInTable.QueryAsync<CheckInEntity>(filter: filter);

            var checkIns = new List<CheckInEntity>();
            
            await foreach (var page in query.AsPages())
            {
                checkIns.AddRange(page.Values);
            }

            return checkIns
                .OrderByDescending(c => c.CheckInTimestamp)
                .Select(c => c.ToModel())
                .ToList();
        }

        /// <summary>
        /// Get recent check-ins across all users (for diagnostic purposes only)
        /// </summary>
        public async Task<List<CheckInRecord>> GetRecentCheckInsAsync(int maxResults = 10)
        {
            // Query for all check-ins, without filtering by user
            var query = _checkInTable.QueryAsync<CheckInEntity>();
            var checkIns = new List<CheckInEntity>();
            
            await foreach (var page in query.AsPages())
            {
                checkIns.AddRange(page.Values);
                
                // Early exit if we have enough records
                if (checkIns.Count >= maxResults)
                {
                    break;
                }
            }
            
            return checkIns
                .OrderByDescending(c => c.CheckInTimestamp)
                .Take(maxResults)
                .Select(c => c.ToModel())
                .ToList();
        }

        /// <summary>
        /// Test storage connectivity by creating and deleting a test record
        /// </summary>
        public async Task<string> TestStorageConnectionAsync()
        {
            try
            {
                // Create table if it doesn't exist
                await _checkInTable.CreateIfNotExistsAsync();

                var testId = $"test-{Guid.NewGuid()}";
                var testEntity = new CheckInEntity
                {
                    PartitionKey = "test",
                    RowKey = testId,
                    UserId = "test",
                    UserDisplayName = "Test User",
                    UserEmail = "test@example.com",
                    CheckInTimestamp = DateTimeOffset.UtcNow,
                    Status = "OK",
                    State = "Submitted",
                    Notes = "",
                    HeadcountCampaignId = "",
                    AcknowledgedByUserId = "",
                    ResolvedByUserId = "",
                    LocationPrecision = "None"
                };

                // Try to create the test entity
                await _checkInTable.AddEntityAsync(testEntity);
                
                // If successful, clean it up
                await _checkInTable.DeleteEntityAsync("test", testId);
                
                return "Storage connection test successful - created and deleted test record";
            }
            catch (Exception ex)
            {
                throw new Exception($"Storage connection test failed: {ex.Message}", ex);
            }
        }
    }
}