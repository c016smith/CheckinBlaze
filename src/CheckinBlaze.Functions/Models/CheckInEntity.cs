using CheckinBlaze.Functions.Models;
using CheckinBlaze.Shared.Models;
using System;

namespace CheckinBlaze.Functions.Models
{
    /// <summary>
    /// Represents a check-in record stored in Azure Table Storage
    /// </summary>
    public class CheckInEntity : BaseTableEntity
    {
        /// <summary>
        /// Azure AD user identifier
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// User's display name
        /// </summary>
        public string UserDisplayName { get; set; }
        
        /// <summary>
        /// Email of the user who checked in
        /// </summary>
        public string UserEmail { get; set; }
        
        /// <summary>
        /// Check-in timestamp in UTC
        /// </summary>
        public DateTimeOffset CheckInTimestamp { get; set; }
        
        /// <summary>
        /// Latitude coordinate of the check-in location
        /// </summary>
        public double? Latitude { get; set; }
        
        /// <summary>
        /// Longitude coordinate of the check-in location
        /// </summary>
        public double? Longitude { get; set; }
        
        /// <summary>
        /// Location accuracy level (city-wide or precise)
        /// </summary>
        public string LocationPrecision { get; set; }
        
        /// <summary>
        /// Safety status of the employee
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Optional notes provided by the employee during check-in
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Current state of the check-in in the workflow
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// ID of the headcount campaign this check-in is associated with (if any)
        /// </summary>
        public string HeadcountCampaignId { get; set; }
        
        /// <summary>
        /// ID of the user who acknowledged the check-in (if State is Acknowledged)
        /// </summary>
        public string AcknowledgedByUserId { get; set; }
        
        /// <summary>
        /// Timestamp when the check-in was acknowledged
        /// </summary>
        public DateTimeOffset? AcknowledgedTimestamp { get; set; }
        
        /// <summary>
        /// ID of the user who resolved the check-in (if State is Resolved)
        /// </summary>
        public string ResolvedByUserId { get; set; }
        
        /// <summary>
        /// Timestamp when the check-in was resolved
        /// </summary>
        public DateTimeOffset? ResolvedTimestamp { get; set; }

        /// <summary>
        /// Create a new CheckInEntity from a CheckInRecord
        /// </summary>
        public static CheckInEntity FromModel(CheckInRecord model)
        {
            string id = model.Id ?? Guid.NewGuid().ToString();
            
            return new CheckInEntity
            {
                PartitionKey = model.UserId,
                RowKey = id,
                UserId = model.UserId,
                UserDisplayName = model.UserDisplayName,
                UserEmail = model.UserEmail,
                CheckInTimestamp = model.Timestamp,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                LocationPrecision = model.LocationPrecision.ToString(),
                Status = model.Status.ToString(),
                Notes = model.Notes,
                State = model.State.ToString(),
                HeadcountCampaignId = model.HeadcountCampaignId,
                AcknowledgedByUserId = model.AcknowledgedByUserId,
                AcknowledgedTimestamp = model.AcknowledgedTimestamp,
                ResolvedByUserId = model.ResolvedByUserId,
                ResolvedTimestamp = model.ResolvedTimestamp
            };
        }

        /// <summary>
        /// Convert this entity to a CheckInRecord model
        /// </summary>
        public CheckInRecord ToModel()
        {
            return new CheckInRecord
            {
                Id = RowKey,
                UserId = UserId,
                UserDisplayName = UserDisplayName,
                UserEmail = UserEmail,
                Timestamp = CheckInTimestamp,
                Latitude = Latitude,
                Longitude = Longitude,
                LocationPrecision = Enum.Parse<LocationPrecision>(LocationPrecision),
                Status = Enum.Parse<SafetyStatus>(Status),
                Notes = Notes,
                State = Enum.Parse<CheckInState>(State),
                HeadcountCampaignId = HeadcountCampaignId,
                AcknowledgedByUserId = AcknowledgedByUserId,
                AcknowledgedTimestamp = AcknowledgedTimestamp,
                ResolvedByUserId = ResolvedByUserId,
                ResolvedTimestamp = ResolvedTimestamp
            };
        }
    }
}