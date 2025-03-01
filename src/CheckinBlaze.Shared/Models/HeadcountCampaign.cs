using System;
using System.Collections.Generic;

namespace CheckinBlaze.Shared.Models
{
    /// <summary>
    /// Represents a headcount campaign initiated by a manager or team lead
    /// </summary>
    public class HeadcountCampaign
    {
        /// <summary>
        /// Unique identifier for the headcount campaign
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Title of the headcount campaign
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description or reason for the headcount campaign
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Azure AD user identifier of the person who initiated the campaign
        /// </summary>
        public string InitiatedByUserId { get; set; }
        
        /// <summary>
        /// Display name of the person who initiated the campaign
        /// </summary>
        public string InitiatedByDisplayName { get; set; }
        
        /// <summary>
        /// User Principal Name of the person who initiated the campaign
        /// </summary>
        public string InitiatedByUPN { get; set; }
        
        /// <summary>
        /// Timestamp when the campaign was created
        /// </summary>
        public DateTimeOffset CreatedTimestamp { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Optional expiration timestamp for the campaign
        /// </summary>
        public DateTimeOffset? ExpiresTimestamp { get; set; }
        
        /// <summary>
        /// Current status of the campaign
        /// </summary>
        public HeadcountCampaignStatus Status { get; set; } = HeadcountCampaignStatus.Active;
        
        /// <summary>
        /// Collection of user IDs who were targeted by this campaign
        /// </summary>
        public List<string> TargetedUserIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Collection of user IDs who have responded to the campaign
        /// </summary>
        public List<string> RespondedUserIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Collection of user IDs who need assistance
        /// </summary>
        public List<string> NeedAssistanceUserIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Collection of user IDs who have been confirmed safe
        /// </summary>
        public List<string> SafeUserIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Optional notes or updates about the campaign
        /// </summary>
        public string Notes { get; set; }
    }
    
    /// <summary>
    /// Defines the status options for a headcount campaign
    /// </summary>
    public enum HeadcountCampaignStatus
    {
        /// <summary>
        /// Campaign is currently active
        /// </summary>
        Active = 0,
        
        /// <summary>
        /// Campaign has been paused
        /// </summary>
        Paused = 1,
        
        /// <summary>
        /// Campaign has been completed
        /// </summary>
        Completed = 2,
        
        /// <summary>
        /// Campaign has expired
        /// </summary>
        Expired = 3,
        
        /// <summary>
        /// Campaign was cancelled
        /// </summary>
        Cancelled = 4
    }
}