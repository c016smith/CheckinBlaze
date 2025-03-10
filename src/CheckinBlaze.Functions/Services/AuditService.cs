using Azure.Data.Tables;
using CheckinBlaze.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Services
{
    /// <summary>
    /// Service for logging audit events
    /// </summary>
    public class AuditService
    {
        private readonly TableClient _auditLogTable;

        public AuditService(TableClient auditLogTable)
        {
            _auditLogTable = auditLogTable;
        }

        /// <summary>
        /// Log an action to the audit log
        /// </summary>
        public async Task LogActionAsync(
            string userId, 
            string userDisplayName, 
            AuditActionType actionType, 
            string entityType, 
            string entityId, 
            string? previousState = null, 
            string? newState = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                UserDisplayName = userDisplayName,
                Timestamp = DateTimeOffset.UtcNow,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                ChangeDescription = GetChangeDescription(actionType, entityType),
                PreviousState = previousState,
                NewState = newState,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            // Create a table entity for the audit log using entity type partitioning
            var entity = new TableEntity
            {
                // Use entity type as partition key for easier querying by type
                PartitionKey = entityType,
                RowKey = auditLog.Id,
                ["UserId"] = userId,
                ["UserDisplayName"] = userDisplayName,
                ["Timestamp"] = auditLog.Timestamp,
                ["ActionType"] = actionType.ToString(),
                ["EntityType"] = entityType,
                ["EntityId"] = entityId,
                ["ChangeDescription"] = auditLog.ChangeDescription,
                ["PreviousState"] = previousState,
                ["NewState"] = newState,
                ["IpAddress"] = ipAddress,
                ["UserAgent"] = userAgent
            };

            await _auditLogTable.AddEntityAsync(entity);
        }

        /// <summary>
        /// Get audit logs for a specific entity
        /// </summary>
        public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, string entityId)
        {
            var auditLogs = new List<AuditLog>();
            
            // Query across all partitions but filter by entity type and ID
            var filter = $"EntityType eq '{entityType}' and EntityId eq '{entityId}'";
            var query = _auditLogTable.QueryAsync<TableEntity>(filter: filter);
            
            await foreach (var page in query.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    auditLogs.Add(MapEntityToAuditLog(entity));
                }
            }

            // Sort by timestamp descending (most recent first)
            auditLogs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            
            return auditLogs;
        }

        /// <summary>
        /// Get recent audit logs for the system
        /// </summary>
        public async Task<List<AuditLog>> GetRecentAuditLogsAsync(int maxResults = 100)
        {
            var auditLogs = new List<AuditLog>();
            
            // Get the current and previous month's partition keys
            var currentMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            var previousMonth = DateTimeOffset.UtcNow.AddMonths(-1).ToString("yyyy-MM");
            
            // Query only the last two months of data for better performance
            var filter = $"PartitionKey ge '{previousMonth}' and PartitionKey le '{currentMonth}'";
            var query = _auditLogTable.QueryAsync<TableEntity>(filter: filter);
            
            await foreach (var page in query.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    auditLogs.Add(MapEntityToAuditLog(entity));
                }
            }

            // Sort by timestamp descending (most recent first)
            auditLogs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            
            // Take only the requested number of results
            if (auditLogs.Count > maxResults)
            {
                auditLogs = auditLogs.GetRange(0, maxResults);
            }
            
            return auditLogs;
        }

        private AuditLog MapEntityToAuditLog(TableEntity entity)
        {
            return new AuditLog
            {
                Id = entity.RowKey,
                UserId = entity.GetString("UserId"),
                UserDisplayName = entity.GetString("UserDisplayName"),
                Timestamp = entity.GetDateTimeOffset("Timestamp") ?? DateTimeOffset.UtcNow,
                ActionType = Enum.Parse<AuditActionType>(entity.GetString("ActionType")),
                EntityType = entity.GetString("EntityType"),
                EntityId = entity.GetString("EntityId"),
                ChangeDescription = entity.GetString("ChangeDescription"),
                PreviousState = entity.GetString("PreviousState"),
                NewState = entity.GetString("NewState"),
                IpAddress = entity.GetString("IpAddress"),
                UserAgent = entity.GetString("UserAgent")
            };
        }

        private string GetChangeDescription(AuditActionType actionType, string entityType)
        {
            return actionType switch
            {
                AuditActionType.Create => $"Created new {entityType}",
                AuditActionType.Update => $"Updated {entityType}",
                AuditActionType.Delete => $"Deleted {entityType}",
                AuditActionType.Login => "User logged in",
                AuditActionType.Logout => "User logged out",
                AuditActionType.CheckIn => "User submitted a check-in",
                AuditActionType.HeadcountInitiated => "Headcount campaign initiated",
                AuditActionType.CheckInAcknowledged => "Check-in acknowledged",
                AuditActionType.CheckInResolved => "Check-in resolved",
                _ => $"{actionType} action on {entityType}"
            };
        }
    }
}