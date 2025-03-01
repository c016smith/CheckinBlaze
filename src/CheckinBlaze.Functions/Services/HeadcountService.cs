using Azure.Data.Tables;
using CheckinBlaze.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Services
{
    /// <summary>
    /// Service for managing headcount campaigns
    /// </summary>
    public class HeadcountService
    {
        private readonly TableClient _campaignsTable;
        private readonly AuditService _auditService;

        public HeadcountService(TableClient campaignsTable, AuditService auditService)
        {
            _campaignsTable = campaignsTable;
            _auditService = auditService;
        }

        /// <summary>
        /// Create a new headcount campaign
        /// </summary>
        public async Task<HeadcountCampaign> CreateCampaignAsync(HeadcountCampaign campaign, string requestorId, string requestorDisplayName)
        {
            // Validate the campaign
            if (string.IsNullOrEmpty(campaign.Title))
            {
                throw new ArgumentException("Campaign title is required");
            }

            if (campaign.TargetedUserIds == null || !campaign.TargetedUserIds.Any())
            {
                throw new ArgumentException("At least one targeted user ID is required");
            }

            // Generate a new ID if one wasn't provided
            if (string.IsNullOrEmpty(campaign.Id))
            {
                campaign.Id = Guid.NewGuid().ToString();
            }

            // Set required fields
            campaign.InitiatedByUserId = requestorId;
            campaign.InitiatedByDisplayName = requestorDisplayName;
            campaign.CreatedTimestamp = DateTimeOffset.UtcNow;
            campaign.Status = HeadcountCampaignStatus.Active;
            
            // Initialize collections if they're null
            campaign.RespondedUserIds ??= new List<string>();
            campaign.NeedAssistanceUserIds ??= new List<string>();
            campaign.SafeUserIds ??= new List<string>();

            // Create a table entity for the campaign
            var entity = new TableEntity
            {
                PartitionKey = requestorId,  // Partition by the initiator's ID
                RowKey = campaign.Id,
                ["Title"] = campaign.Title,
                ["Description"] = campaign.Description,
                ["InitiatedByUserId"] = campaign.InitiatedByUserId,
                ["InitiatedByDisplayName"] = campaign.InitiatedByDisplayName,
                ["CreatedTimestamp"] = campaign.CreatedTimestamp,
                ["ExpiresTimestamp"] = campaign.ExpiresTimestamp,
                ["Status"] = campaign.Status.ToString(),
                ["TargetedUserIds"] = JsonSerializer.Serialize(campaign.TargetedUserIds),
                ["RespondedUserIds"] = JsonSerializer.Serialize(campaign.RespondedUserIds),
                ["NeedAssistanceUserIds"] = JsonSerializer.Serialize(campaign.NeedAssistanceUserIds),
                ["SafeUserIds"] = JsonSerializer.Serialize(campaign.SafeUserIds),
                ["Notes"] = campaign.Notes
            };

            await _campaignsTable.AddEntityAsync(entity);

            // Log the action
            await _auditService.LogActionAsync(
                requestorId,
                requestorDisplayName,
                AuditActionType.HeadcountInitiated,
                "HeadcountCampaign",
                campaign.Id,
                null,
                JsonSerializer.Serialize(campaign));

            return campaign;
        }

        /// <summary>
        /// Get a headcount campaign by ID
        /// </summary>
        public async Task<HeadcountCampaign> GetCampaignAsync(string initiatorId, string campaignId)
        {
            var response = await _campaignsTable.GetEntityIfExistsAsync<TableEntity>(initiatorId, campaignId);
            
            if (response.HasValue)
            {
                return MapEntityToCampaign(response.Value);
            }
            
            return null;
        }

        /// <summary>
        /// Get active headcount campaigns initiated by a user
        /// </summary>
        public async Task<List<HeadcountCampaign>> GetActiveCampaignsAsync(string initiatorId)
        {
            var query = _campaignsTable.QueryAsync<TableEntity>(
                filter: $"PartitionKey eq '{initiatorId}' and Status eq '{HeadcountCampaignStatus.Active}'");
            
            var campaigns = new List<HeadcountCampaign>();
            
            await foreach (var page in query.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    campaigns.Add(MapEntityToCampaign(entity));
                }
            }
            
            // Sort by creation timestamp descending (most recent first)
            return campaigns.OrderByDescending(c => c.CreatedTimestamp).ToList();
        }

        /// <summary>
        /// Get all headcount campaigns for a user (both initiated by and targeted at)
        /// </summary>
        public async Task<List<HeadcountCampaign>> GetAllCampaignsForUserAsync(string userId)
        {
            // Get campaigns initiated by this user
            var initiatedQuery = _campaignsTable.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{userId}'");
            
            var campaigns = new List<HeadcountCampaign>();
            
            await foreach (var page in initiatedQuery.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    campaigns.Add(MapEntityToCampaign(entity));
                }
            }
            
            // Get campaigns targeted at this user (requires full scan)
            var allCampaignsQuery = _campaignsTable.QueryAsync<TableEntity>();
            await foreach (var page in allCampaignsQuery.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    if (entity.PartitionKey != userId) // Skip already processed campaigns
                    {
                        var targetedUserIds = JsonSerializer.Deserialize<List<string>>(
                            entity.GetString("TargetedUserIds") ?? "[]");
                        
                        if (targetedUserIds.Contains(userId))
                        {
                            campaigns.Add(MapEntityToCampaign(entity));
                        }
                    }
                }
            }
            
            // Sort by creation timestamp descending (most recent first)
            return campaigns.OrderByDescending(c => c.CreatedTimestamp).ToList();
        }

        /// <summary>
        /// Update the status of a campaign
        /// </summary>
        public async Task<HeadcountCampaign> UpdateCampaignStatusAsync(
            string initiatorId, 
            string campaignId, 
            HeadcountCampaignStatus status,
            string requestorId,
            string requestorDisplayName)
        {
            var response = await _campaignsTable.GetEntityIfExistsAsync<TableEntity>(initiatorId, campaignId);
            
            if (!response.HasValue)
            {
                throw new KeyNotFoundException($"Campaign with ID {campaignId} not found");
            }
            
            var entity = response.Value;
            var previousCampaign = MapEntityToCampaign(entity);
            
            entity["Status"] = status.ToString();
            
            if (status == HeadcountCampaignStatus.Completed || status == HeadcountCampaignStatus.Expired)
            {
                entity["ExpiresTimestamp"] = DateTimeOffset.UtcNow;
            }
            
            await _campaignsTable.UpdateEntityAsync(entity, entity.ETag);
            
            var updatedCampaign = MapEntityToCampaign(entity);
            
            // Log the action
            await _auditService.LogActionAsync(
                requestorId,
                requestorDisplayName,
                AuditActionType.Update,
                "HeadcountCampaign",
                campaignId,
                JsonSerializer.Serialize(previousCampaign),
                JsonSerializer.Serialize(updatedCampaign));
            
            return updatedCampaign;
        }

        /// <summary>
        /// Update a campaign with a user's check-in
        /// </summary>
        public async Task<HeadcountCampaign> UpdateCampaignWithCheckInAsync(
            string initiatorId, 
            string campaignId, 
            string userId, 
            bool needsAssistance)
        {
            var response = await _campaignsTable.GetEntityIfExistsAsync<TableEntity>(initiatorId, campaignId);
            
            if (!response.HasValue)
            {
                throw new KeyNotFoundException($"Campaign with ID {campaignId} not found");
            }
            
            var entity = response.Value;
            var previousCampaign = MapEntityToCampaign(entity);
            
            var respondedUserIds = JsonSerializer.Deserialize<List<string>>(
                entity.GetString("RespondedUserIds") ?? "[]");
            
            var needAssistanceUserIds = JsonSerializer.Deserialize<List<string>>(
                entity.GetString("NeedAssistanceUserIds") ?? "[]");
            
            var safeUserIds = JsonSerializer.Deserialize<List<string>>(
                entity.GetString("SafeUserIds") ?? "[]");
            
            // Add to responded users if not already there
            if (!respondedUserIds.Contains(userId))
            {
                respondedUserIds.Add(userId);
            }
            
            // Add to appropriate status list
            if (needsAssistance)
            {
                if (!needAssistanceUserIds.Contains(userId))
                {
                    needAssistanceUserIds.Add(userId);
                }
                
                // Remove from safe users if present
                safeUserIds.Remove(userId);
            }
            else
            {
                if (!safeUserIds.Contains(userId))
                {
                    safeUserIds.Add(userId);
                }
                
                // Remove from needs assistance if present
                needAssistanceUserIds.Remove(userId);
            }
            
            // Update the entity
            entity["RespondedUserIds"] = JsonSerializer.Serialize(respondedUserIds);
            entity["NeedAssistanceUserIds"] = JsonSerializer.Serialize(needAssistanceUserIds);
            entity["SafeUserIds"] = JsonSerializer.Serialize(safeUserIds);
            
            await _campaignsTable.UpdateEntityAsync(entity, entity.ETag);
            
            var updatedCampaign = MapEntityToCampaign(entity);
            
            // Log the action
            await _auditService.LogActionAsync(
                userId,
                userId,
                AuditActionType.CheckIn,
                "HeadcountCampaign",
                campaignId,
                JsonSerializer.Serialize(previousCampaign),
                JsonSerializer.Serialize(updatedCampaign));
            
            return updatedCampaign;
        }

        private HeadcountCampaign MapEntityToCampaign(TableEntity entity)
        {
            return new HeadcountCampaign
            {
                Id = entity.RowKey,
                Title = entity.GetString("Title"),
                Description = entity.GetString("Description"),
                InitiatedByUserId = entity.GetString("InitiatedByUserId"),
                InitiatedByDisplayName = entity.GetString("InitiatedByDisplayName"),
                CreatedTimestamp = entity.GetDateTimeOffset("CreatedTimestamp") ?? DateTimeOffset.UtcNow,
                ExpiresTimestamp = entity.GetDateTimeOffset("ExpiresTimestamp"),
                Status = Enum.Parse<HeadcountCampaignStatus>(entity.GetString("Status") ?? HeadcountCampaignStatus.Active.ToString()),
                TargetedUserIds = JsonSerializer.Deserialize<List<string>>(entity.GetString("TargetedUserIds") ?? "[]"),
                RespondedUserIds = JsonSerializer.Deserialize<List<string>>(entity.GetString("RespondedUserIds") ?? "[]"),
                NeedAssistanceUserIds = JsonSerializer.Deserialize<List<string>>(entity.GetString("NeedAssistanceUserIds") ?? "[]"),
                SafeUserIds = JsonSerializer.Deserialize<List<string>>(entity.GetString("SafeUserIds") ?? "[]"),
                Notes = entity.GetString("Notes")
            };
        }
    }
}