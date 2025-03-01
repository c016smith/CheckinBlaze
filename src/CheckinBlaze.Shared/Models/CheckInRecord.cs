using System;

namespace CheckinBlaze.Shared.Models
{
    /// <summary>
    /// Represents an employee check-in record during an emergency event
    /// </summary>
    public class CheckInRecord
    {
        /// <summary>
        /// Unique identifier for the check-in record
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
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
        /// User's job title
        /// </summary>
        public string UserJobTitle { get; set; }
        
        /// <summary>
        /// User's department
        /// </summary>
        public string UserDepartment { get; set; }
        
        /// <summary>
        /// User's office location
        /// </summary>
        public string UserOfficeLocation { get; set; }
        
        /// <summary>
        /// Timestamp when the check-in was recorded
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
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
        public LocationPrecision LocationPrecision { get; set; } = LocationPrecision.CityWide;
        
        /// <summary>
        /// Safety status of the employee
        /// </summary>
        public SafetyStatus Status { get; set; } = SafetyStatus.OK;
        
        /// <summary>
        /// Optional notes provided by the employee during check-in
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Current state of the check-in in the workflow
        /// </summary>
        public CheckInState State { get; set; } = CheckInState.Submitted;
        
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
    }
    
    /// <summary>
    /// Defines the level of location precision for a check-in
    /// </summary>
    public enum LocationPrecision
    {
        /// <summary>
        /// City-wide location accuracy (less precise)
        /// </summary>
        CityWide = 0,
        
        /// <summary>
        /// Precise location accuracy
        /// </summary>
        Precise = 1
    }
    
    /// <summary>
    /// Defines the safety status options for a check-in
    /// </summary>
    public enum SafetyStatus
    {
        /// <summary>
        /// User is safe and does not need assistance
        /// </summary>
        OK = 0,
        
        /// <summary>
        /// User needs assistance
        /// </summary>
        NeedsAssistance = 1
    }
    
    /// <summary>
    /// Defines the workflow state of a check-in
    /// </summary>
    public enum CheckInState
    {
        /// <summary>
        /// Check-in has been submitted but not yet acknowledged
        /// </summary>
        Submitted = 0,
        
        /// <summary>
        /// Check-in has been acknowledged by a responder
        /// </summary>
        Acknowledged = 1,
        
        /// <summary>
        /// Check-in has been resolved
        /// </summary>
        Resolved = 2
    }
}