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
        public required string UserId { get; set; }
        
        /// <summary>
        /// User's display name
        /// </summary>
        public required string UserDisplayName { get; set; }
        
        /// <summary>
        /// Email of the user who checked in
        /// </summary>
        public required string UserEmail { get; set; }
        
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
        public required string LocationPrecision { get; set; }
        
        /// <summary>
        /// Safety status of the employee
        /// </summary>
        public required string Status { get; set; }
        
        /// <summary>
        /// Optional notes provided by the employee during check-in
        /// </summary>
        public required string Notes { get; set; }
        
        /// <summary>
        /// Current state of the check-in in the workflow
        /// </summary>
        public required string State { get; set; }
        
        /// <summary>
        /// ID of the headcount campaign this check-in is associated with (if any)
        /// </summary>
        public string? HeadcountCampaignId { get; set; }
        
        /// <summary>
        /// ID of the user who acknowledged the check-in (if State is Acknowledged)
        /// </summary>
        public string? AcknowledgedByUserId { get; set; }
        
        /// <summary>
        /// Timestamp when the check-in was acknowledged
        /// </summary>
        public DateTimeOffset? AcknowledgedTimestamp { get; set; }
        
        /// <summary>
        /// ID of the user who resolved the check-in (if State is Resolved)
        /// </summary>
        public string? ResolvedByUserId { get; set; }
        
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
                // Use user ID as partition key for check-in records
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
                Notes = model.Notes ?? string.Empty,
                State = model.State.ToString(),
                HeadcountCampaignId = model.HeadcountCampaignId ?? string.Empty,
                AcknowledgedByUserId = model.AcknowledgedByUserId ?? string.Empty,
                ResolvedByUserId = model.ResolvedByUserId ?? string.Empty,
                AcknowledgedTimestamp = model.AcknowledgedTimestamp,
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
                LocationPrecision = !string.IsNullOrEmpty(LocationPrecision) 
                    ? (Enum.TryParse<LocationPrecision>(LocationPrecision, true, out var locationPrecision) 
                        ? locationPrecision 
                        : Shared.Models.LocationPrecision.CityWide)
                    : Shared.Models.LocationPrecision.CityWide,
                Status = !string.IsNullOrEmpty(Status) 
                    ? Enum.TryParse<SafetyStatus>(Status, out var status) 
                        ? status 
                        : SafetyStatus.OK
                    : SafetyStatus.OK,
                Notes = Notes ?? string.Empty,
                State = !string.IsNullOrEmpty(State) 
                    ? Enum.TryParse<CheckInState>(State, out var state) 
                        ? state 
                        : CheckInState.Submitted
                    : CheckInState.Submitted,
                HeadcountCampaignId = string.IsNullOrEmpty(HeadcountCampaignId) ? null : HeadcountCampaignId,
                AcknowledgedByUserId = string.IsNullOrEmpty(AcknowledgedByUserId) ? null : AcknowledgedByUserId,
                ResolvedByUserId = string.IsNullOrEmpty(ResolvedByUserId) ? null : ResolvedByUserId,
                AcknowledgedTimestamp = AcknowledgedTimestamp,
                ResolvedTimestamp = ResolvedTimestamp
            };
        }
    }
}